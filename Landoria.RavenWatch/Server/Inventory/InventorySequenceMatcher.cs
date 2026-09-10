using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.RavenWatch.Shared;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventorySequenceMatcher
    {
        internal static List<JObject> Match(List<JObject> events, List<JObject> snapshots)
        {
            var result = new List<JObject>();
            var used = new HashSet<string>();
            foreach (var group in snapshots.Where(e => e["streamId"] != null)
                .GroupBy(e => (string)e["characterId"] + ":" + (string)e["streamId"]))
            {
                var reports = group.ToList();
                result.Add(Verified(reports[0], null, reports[0], "sequence_baseline"));
                for (int i = 1; i < reports.Count; i++)
                    Interval(events, reports[i - 1], reports[i], used, result);
            }
            return result;
        }

        private static void Interval(List<JObject> events, JObject before, JObject after,
            HashSet<string> used, List<JObject> result)
        {
            long start = (long)before["sequence"], end = (long)after["sequence"];
            var movements = events.Where(e => e["items"] == null && e["sequence"] != null
                && Same(e, after) && (string)e["streamId"] == (string)after["streamId"]
                && (long)e["sequence"] > start && (long)e["sequence"] <= end)
                .OrderBy(e => (long)e["sequence"]).ToList();
            if (end < start || end - start != movements.Count
                || movements.Where((e, i) => (long)e["sequence"] != start + i + 1).Any()) return;
            var actual = new Dictionary<string, long>();
            foreach (JObject item in InventoryChanges.Compare(before, after))
                InventoryEventDeltas.Add(actual, item, (long)item["quantityDelta"]);
            if (!Balances(actual, before, after, movements)) return;
            result.Add(Verified(after, before, after, "sequence_inventory_balance"));
            foreach (var movement in movements)
                result.Add(Verified(movement, before, after, "sequence_inventory_balance"));
            Observations(events, movements, before, after, used, result);
        }

        private static bool Balances(Dictionary<string, long> actual, JObject before, JObject after,
            List<JObject> movements)
        {
            var declared = new Dictionary<string, long>();
            var materialKeys = MaterialKeys(before, after, movements);
            foreach (var movement in movements)
            {
                var delta = InventoryEventDeltas.Read(movement, materialKeys);
                if ((string)movement["context"] == "item_crafted" && delta.Count == 0) return false;
                foreach (var pair in delta)
                {
                    declared.TryGetValue(pair.Key, out long count);
                    declared[pair.Key] = count + pair.Value;
                }
            }
            return !(actual.Any(p => !declared.TryGetValue(p.Key, out long value) || value != p.Value)
                || declared.Any(p => p.Value != 0 && !actual.ContainsKey(p.Key)));
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

        private static void Observations(List<JObject> events, List<JObject> movements,
            JObject before, JObject after, HashSet<string> used, List<JObject> result)
        {
            // Keep positive and negative budgets separate even when their net balance is zero.
            var budgets = new Dictionary<string, long>();
            foreach (var movement in movements)
            {
                if ((string)movement["context"] == "item_crafted" || (string)movement["context"] == "item_broken") continue;
                foreach (JObject item in (JArray)movement["changes"])
                {
                    long delta = (long)item["quantityDelta"];
                    string key = InventoryEventDeltas.Key(item) + ":" + Math.Sign(delta);
                    budgets.TryGetValue(key, out long count);
                    budgets[key] = count + Math.Abs(delta);
                }
            }
            foreach (var entry in events.Where(e => Same(e, after) && e["sequence"] == null
                && e["items"] == null && Time(e) >= Time(before) && Time(e) <= Time(after).AddSeconds(10)
                && (e["verification"] == null || (string)e["verification"]["toEventId"] == (string)after["eventId"]))
                .OrderBy(e => e["verification"] == null ? 1 : 0))
            {
                long delta = (long?)entry["quantityDelta"] ?? 0;
                if (delta == 0 || used.Contains((string)entry["eventId"])) continue;
                if (entry["verification"] == null && Ambiguous(events, result, entry, before, after)) continue;
                string key = InventoryEventDeltas.Key(entry) + ":" + Math.Sign(delta);
                if (!budgets.TryGetValue(key, out long available) || available < Math.Abs(delta)) continue;
                budgets[key] -= Math.Abs(delta);
                used.Add((string)entry["eventId"]);
                result.Add(Verified(entry, before, after, "server_observation_matches_sequence_interval"));
            }
        }

        private static bool Ambiguous(List<JObject> events, List<JObject> matched, JObject observation,
            JObject before, JObject after)
        {
            string key = InventoryEventDeltas.Key(observation);
            int sign = Math.Sign((long)observation["quantityDelta"]);
            // Prefer links established during this review, then persisted links from earlier reviews.
            var known = matched.Concat(events).GroupBy(e => (string)e["eventId"])
                .Select(group => group.First()).ToList();
            return known.Any(e => Same(e, after) && e["items"] == null && e["sequence"] != null
                && Time(e) <= Time(observation) && Time(e) >= Time(observation).AddSeconds(-10)
                && ((string)e["streamId"] != (string)after["streamId"]
                    || (long)e["sequence"] <= (long)before["sequence"] || (long)e["sequence"] > (long)after["sequence"])
                && (string)e["context"] != "item_crafted" && (string)e["context"] != "item_broken"
                && e["changes"] is JArray changes && changes.Any(item => InventoryEventDeltas.Key(item) == key
                    && Math.Sign((long)item["quantityDelta"]) == sign)
                && UnmatchedQuantity(known, e, key, sign) > 0);
        }

        private static long UnmatchedQuantity(List<JObject> known, JObject movement, string key, int sign)
        {
            string interval = (string)movement["verification"]?["toEventId"];
            if (interval == null) return Quantity(movement, key, sign);
            long declared = 0, observed = 0;
            foreach (var entry in known.Where(e => (string)e["verification"]?["toEventId"] == interval))
            {
                if ((string)entry["verification"]?["basis"] == "sequence_inventory_balance"
                    && entry["items"] == null && (string)entry["context"] != "item_crafted"
                    && (string)entry["context"] != "item_broken")
                    declared += Quantity(entry, key, sign);
                if ((string)entry["verification"]?["basis"] != "server_observation_matches_sequence_interval") continue;
                long delta = (long?)entry["quantityDelta"] ?? 0;
                if (InventoryEventDeltas.Key(entry) == key && Math.Sign(delta) == sign)
                    observed += Math.Abs(delta);
            }
            return Math.Max(0, declared - observed);
        }

        private static long Quantity(JObject movement, string key, int sign)
            => (movement["changes"] as JArray ?? new JArray())
                .Where(item => InventoryEventDeltas.Key(item) == key && Math.Sign((long)item["quantityDelta"]) == sign)
                .Sum(item => Math.Abs((long)item["quantityDelta"]));

        private static bool Same(JObject a, JObject b) => JToken.DeepEquals(a["characterId"], b["characterId"]);
        private static DateTime Time(JObject entry) => entry["utc"].Value<DateTime>();
        private static JObject Verified(JObject entry, JObject before, JObject after, string basis)
        {
            var copy = (JObject)entry.DeepClone();
            copy["verification"] = new JObject { ["fromEventId"] = before?["eventId"]?.DeepClone(),
                ["toEventId"] = after["eventId"].DeepClone(), ["basis"] = basis,
                ["streamId"] = after["streamId"].DeepClone(), ["fromSequence"] = before?["sequence"]?.DeepClone(),
                ["toSequence"] = after["sequence"].DeepClone(), ["verifiedUtc"] = DateTime.UtcNow };
            return copy;
        }
    }
}
