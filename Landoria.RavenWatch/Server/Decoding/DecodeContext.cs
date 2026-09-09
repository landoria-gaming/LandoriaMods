using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Decoding
{
    internal sealed class DecodeContext
    {
        internal readonly string Direction;
        internal readonly Func<int, string[]> PrefabComponents;
        internal readonly Func<long, uint, string[]> ObjectComponents;
        internal int Values;
        internal DecodeContext(string direction, Func<int, string[]> prefabs, Func<long, uint, string[]> objects)
        { Direction = direction; PrefabComponents = prefabs; ObjectComponents = objects; }

        internal void Count()
        {
            if (++Values > 8 * 1024 * 1024) throw new InvalidDataException("RPC exceeds decoded value limit.");
        }
    }

    internal sealed class DecodeScope
    {
        internal readonly JObject Value;
        internal readonly DecodeScope Parent;
        internal readonly JObject Root;
        internal DecodeScope(JObject value, DecodeScope parent = null)
        { Value = value; Parent = parent; Root = parent?.Root ?? value; }

        internal JToken Find(string name, DecodeContext context)
        {
            if (name == "context.direction") return context.Direction;
            if (name.StartsWith("$root.")) return Root[name.Substring(6)];
            for (var scope = this; scope != null; scope = scope.Parent)
                if (scope.Value.TryGetValue(name, out var value)) return value;
            throw new InvalidDataException("Unknown schema field: " + name);
        }

        internal bool Matches(JToken condition, DecodeContext context)
        {
            if (condition == null) return true;
            var actual = Find((string)condition["field"], context);
            switch ((string)condition["op"])
            {
                case "bitSet": return ((long)actual & (long)condition["mask"]) != 0;
                case "equals": return JToken.DeepEquals(actual, condition["value"]);
                case "gte": return (double)actual >= (double)condition["value"];
                default: throw new InvalidDataException("Unknown schema predicate.");
            }
        }
    }
}
