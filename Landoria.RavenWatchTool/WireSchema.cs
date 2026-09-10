using System.Text.Json.Nodes;

namespace Landoria.RavenWatchTool;

internal static class WireSchema
{
    internal static JsonObject Field(string name, string type) => new() { ["name"] = name, ["type"] = type };
    internal static JsonObject Named(string name, JsonObject schema)
    { var result = (JsonObject)schema.DeepClone(); result["name"] = name; return result; }
    internal static JsonObject Struct(string source, params JsonObject[] fields) => new()
    { ["type"] = "struct", ["source"] = source, ["fields"] = new JsonArray(fields.Cast<JsonNode>().ToArray()) };
    internal static JsonObject Array(string name, JsonNode item, string count = "int32") => new()
    { ["name"] = name, ["type"] = "array", ["countPrefix"] = count, ["item"] = item };
    internal static JsonObject When(JsonObject field, string op, string selector, JsonNode value)
    {
        field["when"] = new JsonObject { ["op"] = op, ["field"] = selector,
            [op == "bitSet" ? "mask" : "value"] = value };
        return field;
    }
    internal static JsonObject Flag(string name, string type, int mask, JsonNode? fallback = null,
        string selector = "flags")
    {
        var result = When(Field(name, type), "bitSet", selector, JsonValue.Create(mask)!);
        if (fallback != null) result["default"] = fallback;
        return result;
    }
    internal static JsonObject Version(string type, params int[] allowed) => new()
    { ["name"] = "version", ["type"] = type, ["allowedValues"] = new JsonArray(allowed.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray()) };
    internal static JsonObject Compressed(JsonObject schema) => new()
    { ["type"] = "compressed", ["codec"] = "gzip", ["schema"] = schema };
    internal static JsonObject Strings(string name, string count = "int32") => Array(name,
        Struct("string pairs", Field("key", "string"), Field("value", "string")), count);
}
