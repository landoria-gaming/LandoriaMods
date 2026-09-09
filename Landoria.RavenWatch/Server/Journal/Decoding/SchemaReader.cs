using System;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Journal.Decoding
{
    internal sealed class SchemaReader
    {
        private readonly RpcDefinitions definitions;
        private readonly DecodeContext context;
        internal SchemaReader(RpcDefinitions definitions, DecodeContext context)
        { this.definitions = definitions; this.context = context; }

        internal JToken Read(WireInput input, JToken specification, DecodeScope scope = null, int depth = 0)
        {
            if (depth > 64) throw new InvalidDataException("RPC nesting limit exceeded.");
            context.Count();
            var schema = specification is JObject obj ? obj : new JObject { ["type"] = specification };
            string type = (string)schema["type"];
            if (IsPrimitive(type)) return Scalar(input, type);
            if (definitions.Types[type] is JObject alias && type != "bytes")
                return Read(input, alias, scope, depth + 1);
            switch (type)
            {
                case "integer": case "float": case "boolean": case "string": case "character":
                    throw new InvalidDataException("Primitive must be referenced by its declared name.");
                case "bytes": return BinaryValue(input, scope, depth);
                case "struct": return Structure(input, schema, scope, depth);
                case "array": return Array(input, schema, scope, depth);
                case "repeatUntil": return Repeat(input, schema, scope, depth);
                case "package": return Package(input, schema, scope, depth);
                case "compressed": return Compressed(input, schema, depth);
                case "directionSwitch": return Read(input, schema["variants"][context.Direction], scope, depth + 1);
                default: throw new InvalidDataException("Unsupported schema type: " + type);
            }
        }

        private static bool IsPrimitive(string type) => type == "int16" || type == "uint16" || type == "int32"
            || type == "uint32" || type == "int64" || type == "uint8" || type == "float32"
            || type == "float64" || type == "boolean" || type == "string" || type == "char";

        private static JToken Scalar(WireInput input, string type)
        {
            object value = input.Primitive(type);
            if (value is float f && (float.IsNaN(f) || float.IsInfinity(f)))
                return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new JValue(value);
        }

        private JObject Structure(WireInput input, JObject schema, DecodeScope parent, int depth)
        {
            var result = new JObject();
            Fields(input, (JArray)schema["fields"], new DecodeScope(result, parent), depth);
            return result;
        }

        private void Fields(WireInput input, JArray fields, DecodeScope scope, int depth)
        {
            foreach (JObject field in fields)
            {
                string name = (string)field["name"];
                if (!scope.Matches(field["when"], context))
                {
                    if (field["default"] != null) scope.Value[name] = field["default"].DeepClone();
                    continue;
                }
                JToken value = Read(input, field, scope, depth + 1);
                if (field["allowedValues"] is JArray allowed && !Contains(allowed, value))
                    throw new InvalidDataException("Unsupported version/value for field " + name);
                scope.Value[name] = value;
                if (field["bits"] is JObject bits)
                    foreach (var flag in bits.Properties()) scope.Value[flag.Name] = ((long)value & (long)flag.Value) != 0;
                if (field["derived"] is JObject derived)
                    foreach (var entry in derived.Properties())
                        scope.Value[entry.Name] = ((long)value >> (int)entry.Value["shift"]) & (long)entry.Value["mask"];
            }
        }

        private static bool Contains(JArray values, JToken value)
        {
            foreach (var candidate in values) if (JToken.DeepEquals(candidate, value)) return true;
            return false;
        }

        private JToken Array(WireInput input, JObject schema, DecodeScope scope, int depth)
        {
            int count = schema["countPrefix"] == null ? checked((int)input.Remaining)
                : Convert.ToInt32(input.Primitive((string)schema["countPrefix"]));
            if (count < 0 || count > input.Remaining) throw new InvalidDataException("Invalid RPC array length.");
            if ((string)(schema["item"] as JValue) == "boolean" && count > 1024)
                return BooleanRuns(input, count);
            var result = new JArray();
            for (int i = 0; i < count; i++) result.Add(Read(input, schema["item"], scope, depth + 1));
            return result;
        }

        private JToken BooleanRuns(WireInput input, int count)
        {
            var runs = new JArray();
            bool previous = false;
            int length = 0;
            for (int i = 0; i < count; i++)
            {
                context.Count();
                bool value = (bool)input.Primitive("boolean");
                if (length != 0 && value != previous) { runs.Add(new JArray(previous, length)); length = 0; }
                previous = value;
                length++;
            }
            if (length != 0) runs.Add(new JArray(previous, length));
            return new JObject { ["encoding"] = "boolean_runs", ["count"] = count, ["runs"] = runs };
        }

        private JArray Repeat(WireInput input, JObject schema, DecodeScope parent, int depth)
        {
            var result = new JArray();
            while (true)
            {
                context.Count();
                long start = input.Position;
                var scope = new DecodeScope(new JObject(), parent);
                Fields(input, (JArray)schema["prefix"], scope, depth);
                if (scope.Matches(schema["stopWhen"], context)) return result;
                Fields(input, (JArray)schema["fields"], scope, depth);
                if (input.Position <= start) throw new InvalidDataException("Non-progressing repetition.");
                result.Add(scope.Value);
            }
        }

        private JToken Package(WireInput input, JObject schema, DecodeScope scope, int depth)
        {
            byte[] bytes = input.Bytes((int)input.Primitive("int32"));
            using (var body = new WireInput(bytes))
            {
                JToken result;
                if (schema["schemaDispatch"] != null)
                {
                    var target = (JObject)scope.Find("targetZdo", context);
                    bool isNone = (string)target["userId"] == "0" && (uint)target["id"] == 0;
                    string[] components = isNone ? null : context.ObjectComponents?.Invoke((long)target["userId"], (uint)target["id"]);
                    result = DecodeMethod(body, isNone ? "routed" : "object", (int)scope.Find("methodHash", context), components, depth);
                }
                else
                {
                    string name = (string)schema["schema"];
                    var selected = (string)schema["registry"] == "packages" ? definitions.Packages[name] : definitions.Types[name];
                    result = Read(body, selected ?? throw new InvalidDataException("Missing package schema: " + name), null, depth + 1);
                }
                body.RequireEnd();
                return result;
            }
        }

        internal JObject DecodeMethod(WireInput input, string transport, int hash, string[] components = null, int depth = 0)
        {
            var candidates = definitions.Candidates(transport, hash, components);
            if (candidates.Count != 1) throw new InvalidDataException($"Ambiguous {transport} RPC {hash}: target component required.");
            JObject method = candidates[0];
            return new JObject { ["name"] = method["name"], ["methodHash"] = hash,
                ["transport"] = transport, ["definitionId"] = method["id"],
                ["arguments"] = Read(input, method["wireSchema"], null, depth + 1) };
        }

        private JToken Compressed(WireInput input, JObject schema, int depth)
        {
            if ((string)schema["codec"] != "gzip") throw new InvalidDataException("Unsupported compression codec.");
            using (var source = new MemoryStream(input.Bytes((int)input.Remaining)))
            using (var gzip = new GZipStream(source, CompressionMode.Decompress))
            using (var expanded = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                int count;
                while ((count = gzip.Read(buffer, 0, buffer.Length)) != 0)
                {
                    if (expanded.Length + count > WireInput.MaximumBytes) throw new InvalidDataException("Decompression limit exceeded.");
                    expanded.Write(buffer, 0, count);
                }
                using (var body = new WireInput(expanded.ToArray()))
                {
                    var result = Read(body, schema["schema"], null, depth + 1);
                    body.RequireEnd();
                    return result;
                }
            }
        }

        private JToken BinaryValue(WireInput input, DecodeScope scope, int depth)
        {
            byte[] bytes = input.Bytes((int)input.Primitive("int32"));
            if (scope?.Value["keyHash"] == null) return new JArray(System.Array.ConvertAll(bytes, b => (int)b));
            string key = (string)scope.Value["keyHash"];
            var schema = definitions.BinaryValues?[key];
            if (schema == null) throw new InvalidDataException("Unknown ZDO binary key: " + key);
            if ((string)schema["type"] == "componentSwitch")
            {
                int prefab = (int)scope.Find("prefabHash", context);
                string[] components = context.PrefabComponents?.Invoke(prefab);
                schema = SelectComponent(schema, components);
            }
            using (var body = new WireInput(bytes))
            {
                var result = Read(body, schema, null, depth + 1);
                body.RequireEnd();
                return result;
            }
        }

        private static JToken SelectComponent(JToken schema, string[] components)
        {
            JToken selected = null;
            foreach (var variant in ((JObject)schema["variants"]).Properties())
                if (components != null && System.Array.IndexOf(components, variant.Name) >= 0)
                {
                    if (selected != null) throw new InvalidDataException("Ambiguous ZDO binary component.");
                    selected = variant.Value;
                }
            return selected ?? throw new InvalidDataException("ZDO binary data requires a known prefab component.");
        }
    }
}
