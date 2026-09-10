using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryEventDeltas
    {
        internal static string Key(JToken item) => new JArray(new[] { "prefabHash", "quality", "variant", "worldLevel" }
            .Select(field => item[field]?.DeepClone())).ToString(Formatting.None);

        internal static Dictionary<string, long> Read(JObject entry, Dictionary<string, long> remaining)
        {
            var result = new Dictionary<string, long>();
            string action = (string)entry["event"] ?? (string)entry["context"];
            if ((long?)entry["quantityDelta"] is long delta && delta != 0) Add(result, entry, delta);
            else if (entry["changes"] is JArray changes)
                foreach (JObject item in changes) Add(result, item, (long)item["quantityDelta"]);
            else if (action == "item_broken" && (bool?)entry["destroyed"] == true)
                Add(result, entry["brokenItem"], -(long)entry["brokenItem"]["quantity"]);
            else if (action == "item_crafted") ReadCraft(entry, result, remaining);
            return result.Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static void ReadCraft(JObject entry, Dictionary<string, long> result, Dictionary<string, long> remaining)
        {
            var recipes = entry["requiredMaterials"]?["recipes"] as JArray;
            if (recipes == null || recipes.Count != 1 || (string)recipes[0]["requirementMode"] != "all") return;
            Add(result, entry["craftedItem"], (long)entry["craftedItem"]["quantity"]);
            if ((bool?)entry["upgrade"] == true)
            {
                var previous = (JObject)entry["craftedItem"].DeepClone();
                previous["quality"] = (int)previous["quality"] - 1;
                if ((int)previous["quality"] < 1) { result.Clear(); return; }
                Add(result, previous, -(long)previous["quantity"]);
            }
            foreach (JObject material in (JArray)recipes[0]["materials"])
            {
                var keys = remaining.Where(pair => pair.Value < 0
                    && (int)JArray.Parse(pair.Key)[0] == (int)material["prefabHash"]).Select(pair => pair.Key).ToList();
                if (keys.Count != 1) { result.Clear(); return; }
                result.TryGetValue(keys[0], out long current);
                result[keys[0]] = current - (long)material["quantity"];
            }
        }

        internal static void Add(Dictionary<string, long> values, JToken item, long quantity)
        {
            string key = Key(item);
            values.TryGetValue(key, out long current);
            values[key] = current + quantity;
        }
    }
}
