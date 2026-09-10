using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.RavenWatch.Shared;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryPendingMatcher
    {
        internal static void Match(List<JObject> candidates, JArray archive, JObject snapshot)
        {
            var before = archive.OfType<JObject>().LastOrDefault(e => e["items"] is JArray
                && JToken.DeepEquals(e["characterId"], snapshot["characterId"])
                && JToken.DeepEquals(e["streamId"], snapshot["streamId"]));
            var remaining = ReadBudget(before, "remainingChanges");
            var observations = InventoryObservationCredits.Load(before);
            if (before != null)
            {
                foreach (JObject change in InventoryChanges.Compare(before, snapshot))
                    InventoryEventDeltas.Add(remaining, change, (long)change["quantityDelta"]);
                var movements = candidates.Where(e => e["streamId"] != null
                    && JToken.DeepEquals(e["streamId"], snapshot["streamId"])).ToList();
                var matched = MatchingMovements(remaining, before, snapshot, movements);
                var keys = MaterialKeys(before, snapshot, movements);
                foreach (string key in remaining.Keys) keys[key] = -1;
                foreach (var entry in matched)
                {
                    foreach (var pair in InventoryEventDeltas.Read(entry, keys)) Add(remaining, pair.Key, -pair.Value);
                    InventoryObservationCredits.Add(observations, entry);
                    Verify(entry, snapshot, "inventory_changes_match");
                }
            }
            else InventoryEvidenceMatcher.FirstInventory(candidates, snapshot, observations);
            InventoryObservationCredits.Match(candidates, observations, snapshot);
            InventoryEvidenceMatcher.Crafts(candidates, archive, snapshot);
            snapshot["reconciliation"] = new JObject { ["remainingChanges"] = JObject.FromObject(remaining),
                ["observationCredits"] = observations };
        }

        private static Dictionary<string, long> ReadBudget(JObject snapshot, string name)
            => snapshot?["reconciliation"]?[name]?.ToObject<Dictionary<string, long>>()
                ?? new Dictionary<string, long>();
        internal static void Add(Dictionary<string, long> budget, string key, long delta)
        {
            budget.TryGetValue(key, out long current);
            budget[key] = current + delta;
        }
        internal static void Verify(JObject entry, JObject snapshot, string basis)
        {
            entry["verification"] = new JObject { ["toEventId"] = snapshot["eventId"].DeepClone(),
                ["basis"] = basis, ["verifiedUtc"] = DateTime.UtcNow };
            entry["verificationStatus"] = "verified";
        }
        private static List<JObject> MatchingMovements(Dictionary<string, long> actual,
            JObject before, JObject after, List<JObject> movements)
        {
            var keys = MaterialKeys(before, after, movements);
            foreach (string key in actual.Keys) keys[key] = -1;
            var deltas = movements.Select(e => InventoryEventDeltas.Read(e, keys)).ToList();
            var declared = new Dictionary<string, long>();
            foreach (var delta in deltas)
                foreach (var pair in delta)
                {
                    declared.TryGetValue(pair.Key, out long count);
                    declared[pair.Key] = count + pair.Value;
                }
            var blocked = new HashSet<string>(actual.Keys.Concat(declared.Keys).Where(key =>
                (actual.TryGetValue(key, out long observed) ? observed : 0)
                != (declared.TryGetValue(key, out long reported) ? reported : 0)));
            var unresolved = new HashSet<int>();
            for (int i = 0; i < movements.Count; i++)
                if ((string)movements[i]["context"] == "item_crafted" && deltas[i].Count == 0)
                {
                    unresolved.Add(i);
                    BlockCraftKeys(movements[i], keys, blocked);
                }
            // A craft or multi-item event is indivisible: propagate a mismatch to its other items.
            int previous;
            do
            {
                previous = blocked.Count;
                foreach (var delta in deltas.Where(d => d.Keys.Any(blocked.Contains)))
                    blocked.UnionWith(delta.Keys);
            } while (blocked.Count != previous);
            return movements.Where((e, i) => !unresolved.Contains(i)
                && !deltas[i].Keys.Any(blocked.Contains)).ToList();
        }

        private static void BlockCraftKeys(JObject movement, Dictionary<string, long> keys, HashSet<string> blocked)
        {
            var hashes = new HashSet<int> { (int)movement["craftedItem"]["prefabHash"] };
            foreach (var recipe in movement["requiredMaterials"]?["recipes"] as JArray ?? new JArray())
                foreach (var material in recipe["materials"] as JArray ?? new JArray())
                    hashes.Add((int)material["prefabHash"]);
            blocked.UnionWith(keys.Keys.Where(key => hashes.Contains((int)JArray.Parse(key)[0])));
        }

        private static Dictionary<string, long> MaterialKeys(JObject before, JObject after, List<JObject> movements)
        {
            var result = new Dictionary<string, long>();
            foreach (var item in ((JArray)before["items"]).Concat((JArray)after["items"]))
                result[InventoryEventDeltas.Key(item)] = -1;
            foreach (var movement in movements)
            {
                if (movement["craftedItem"] is JObject crafted) result[InventoryEventDeltas.Key(crafted)] = -1;
                foreach (var item in movement["changes"] as JArray ?? new JArray())
                    result[InventoryEventDeltas.Key(item)] = -1;
            }
            return result;
        }

    }
}
