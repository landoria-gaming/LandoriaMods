using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryEvidenceMatcher
    {
        internal static void Starting(List<JObject> candidates, JObject snapshot)
        {
            foreach (var group in candidates.Where(e => (bool?)e["startingItem"] == true)
                .GroupBy(InventoryEventDeltas.Key))
            {
                long quantity = ((JArray)snapshot["items"]).Where(e => InventoryEventDeltas.Key(e) == group.Key)
                    .Sum(e => (long)e["quantity"]);
                if (group.Sum(e => (long)e["quantityDelta"]) > quantity) continue;
                foreach (var entry in group) InventoryPendingMatcher.Verify(entry, snapshot, "initial_inventory_presence");
            }
        }

        internal static void FirstInventory(List<JObject> candidates, JObject snapshot, JArray observations)
        {
            Starting(candidates, snapshot);
            var stock = new Dictionary<string, long>();
            foreach (var item in (JArray)snapshot["items"])
                InventoryEventDeltas.Add(stock, item, (long)item["quantity"]);
            foreach (var entry in candidates.Where(e => (bool?)e["startingItem"] == true && e["verification"] != null))
                InventoryEventDeltas.Add(stock, entry, -(long)entry["quantityDelta"]);
            var entries = candidates.Where(e => e["verification"] == null && e["changes"] is JArray changes
                && changes.Count > 0 && changes.All(item => (long)item["quantityDelta"] > 0)
                && JToken.DeepEquals(e["streamId"], snapshot["streamId"])).ToList();
            var totals = new Dictionary<string, long>();
            foreach (var entry in entries)
                foreach (var item in (JArray)entry["changes"])
                    InventoryEventDeltas.Add(totals, item, (long)item["quantityDelta"]);
            foreach (var entry in entries)
            {
                var delta = InventoryEventDeltas.Read(entry, stock);
                if (delta.Keys.Any(key => !stock.TryGetValue(key, out long count) || totals[key] > count)) continue;
                // Presence corroborates entries before the baseline; it does not prove their origin.
                InventoryPendingMatcher.Verify(entry, snapshot, "first_inventory_entries_present");
                InventoryObservationCredits.Add(observations, entry);
            }
        }

        internal static void Crafts(List<JObject> candidates, JArray archive, JObject snapshot)
        {
            var known = archive.OfType<JObject>().Concat(candidates).ToList();
            var used = new HashSet<string>(known.Select(e => (string)e["verification"]?["craftEventId"])
                .Where(id => id != null));
            var recent = new HashSet<string>(archive.OfType<JObject>().Where(e => e["items"] is JArray
                && JToken.DeepEquals(e["characterId"], snapshot["characterId"])
                && JToken.DeepEquals(e["streamId"], snapshot["streamId"]))
                .Reverse().Take(2).Select(e => (string)e["eventId"])) { (string)snapshot["eventId"] };
            var crafts = known.Where(e => (string)e["context"] == "item_crafted" && e["verification"] != null
                && JToken.DeepEquals(e["characterId"], snapshot["characterId"])
                && JToken.DeepEquals(e["streamId"], snapshot["streamId"])
                && recent.Contains((string)e["verification"]?["toEventId"])
                && !used.Contains((string)e["eventId"])).ToList();
            var starts = candidates.Where(e => (string)e["event"] == "craft_started" && e["verification"] == null).ToList();
            var ends = candidates.Where(e => (string)e["event"] == "craft_ended" && e["verification"] == null).ToList();
            // Without timing evidence, multiple possible crafts or signals remain ambiguous.
            if (crafts.Count != 1 || starts.Count != 1 || ends.Count != 1) return;
            Link(starts[0], ends[0], crafts[0], snapshot);
            Link(ends[0], starts[0], crafts[0], snapshot);
        }

        private static void Link(JObject signal, JObject other, JObject craft, JObject snapshot)
        {
            InventoryPendingMatcher.Verify(signal, snapshot, "craft_signals_match_verified_client_craft");
            signal["verification"]["craftEventId"] = craft["eventId"].DeepClone();
            signal["verification"]["pairedSignalEventId"] = other["eventId"].DeepClone();
        }
    }
}
