using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryObservationCredits
    {
        internal static JArray Load(JObject snapshot)
            // Old aggregate quantities have no provenance and cannot be reused safely.
            => snapshot?["reconciliation"]?["observationCredits"] is JArray credits
                ? (JArray)credits.DeepClone() : new JArray();

        internal static void Add(JArray credits, JObject entry)
        {
            if ((string)entry["context"] == "item_crafted" || (string)entry["context"] == "item_broken") return;
            foreach (var item in entry["changes"] as JArray ?? new JArray())
            {
                long delta = (long)item["quantityDelta"];
                string operation = (string)entry["operation"];
                if (delta == 0 || operation == "MoveInventoryToGrave"
                    || (delta < 0 && operation != "DropItem")) continue;
                credits.Add(new JObject { ["sourceEventId"] = entry["eventId"].DeepClone(),
                    ["sourceOperation"] = operation, ["key"] = InventoryEventDeltas.Key(item),
                    ["event"] = delta > 0 ? "pickup" : "drop", ["remaining"] = Math.Abs(delta),
                    ["inventoryAttempts"] = 0 });
            }
        }

        internal static void Match(List<JObject> candidates, JArray credits, JObject snapshot)
        {
            var groups = candidates.Where(e => e["verification"] == null && e["streamId"] == null
                && ((long?)e["quantityDelta"] ?? 0) != 0)
                .GroupBy(e => (string)e["event"] + ":" + InventoryEventDeltas.Key(e));
            foreach (var group in groups)
            {
                var first = group.First();
                string action = (string)first["event"], key = InventoryEventDeltas.Key(first);
                var sources = credits.OfType<JObject>().Where(c => (string)c["event"] == action
                    && (string)c["key"] == key && (int)c["inventoryAttempts"] < 3).ToList();
                long total = group.Sum(e => Math.Abs((long)e["quantityDelta"]));
                if (total > sources.Sum(c => (long)c["remaining"])) continue;
                foreach (var entry in group)
                {
                    if (((long)entry["quantityDelta"] > 0) != (action == "pickup")) continue;
                    InventoryPendingMatcher.Verify(entry, snapshot, "server_observation_matches");
                    entry["verification"]["sources"] = Consume(sources, Math.Abs((long)entry["quantityDelta"]));
                }
            }
            foreach (var credit in credits.OfType<JObject>().ToList())
            {
                credit["inventoryAttempts"] = (int)credit["inventoryAttempts"] + 1;
                if ((long)credit["remaining"] == 0 || (int)credit["inventoryAttempts"] >= 3) credit.Remove();
            }
        }

        private static JArray Consume(List<JObject> sources, long quantity)
        {
            var links = new JArray();
            foreach (var source in sources)
            {
                long used = Math.Min(quantity, (long)source["remaining"]);
                if (used == 0) continue;
                source["remaining"] = (long)source["remaining"] - used;
                links.Add(new JObject { ["eventId"] = source["sourceEventId"].DeepClone(),
                    ["operation"] = source["sourceOperation"].DeepClone(), ["quantity"] = used });
                quantity -= used;
                if (quantity == 0) break;
            }
            return links;
        }
    }
}
