using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Shared
{
    internal static class InventoryChanges
    {
        private static readonly string[] Fields = { "prefabHash", "prefabName", "quality", "variant", "worldLevel" };

        internal static JArray Compare(JObject before, JObject after)
        {
            var entries = new Dictionary<string, JObject>();
            Accumulate(entries, (JArray)before["items"], -1);
            Accumulate(entries, (JArray)after["items"], 1);
            return new JArray(entries.Values.Where(item => (long)item["quantityDelta"] != 0));
        }

        private static void Accumulate(Dictionary<string, JObject> entries, JArray items, int sign)
        {
            foreach (JObject item in items)
            {
                var identity = new JObject();
                foreach (string field in Fields) identity[field] = item[field]?.DeepClone();
                string key = identity.ToString(Formatting.None);
                if (!entries.TryGetValue(key, out var entry))
                {
                    entry = identity;
                    entry["quantityDelta"] = 0L;
                    entries.Add(key, entry);
                }
                entry["quantityDelta"] = (long)entry["quantityDelta"] + sign * (long)(int)item["quantity"];
            }
        }
    }
}
