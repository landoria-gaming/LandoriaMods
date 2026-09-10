using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.RavenWatch.Shared;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryEventMatcher
    {
        internal static List<JObject> Match(List<JObject> events, List<JObject> snapshots)
        {
            var result = InventorySequenceMatcher.Match(events, snapshots);
            foreach (var group in snapshots.GroupBy(e => (string)e["characterId"]))
                Initial(events, group.First(), result);
            foreach (var group in snapshots.Where(e => e["streamId"] == null).GroupBy(e => (string)e["characterId"]))
            {
                var reports = group.ToList();
                for (int i = 0; i < reports.Count; i++)
                    Report(events, i == 0 ? null : reports[i - 1], reports[i],
                        i + 1 < reports.Count ? reports[i + 1] : null, result);
            }
            CraftSignalMatcher.Match(events, result);
            return result;
        }

        private static void Initial(List<JObject> events, JObject report, List<JObject> result)
        {
            var stock = Stock(report);
            var initial = events.Where(e => SameCharacter(e, report)
                && (string)e["event"] == "pickup" && Time(e) < Time(report)).ToList();
            if (initial.Count == 0) return;
            DateTime first = Time(initial[0]);
            foreach (var entry in initial.Where(e => Time(e) == first
                && ((string)e["prefabName"] == "Torch" || (string)e["prefabName"] == "ArmorRagsChest")))
            {
                var changes = InventoryEventDeltas.Read(entry, stock);
                if (!Fits(changes, stock)) continue;
                Consume(changes, stock);
                result.Add(Verified(entry, null, report, "initial_inventory_presence"));
            }
        }

        private static void Report(List<JObject> events, JObject before, JObject after,
            JObject next, List<JObject> result)
        {
            var difference = before == null ? Stock(after) : new Dictionary<string, long>();
            if (before != null)
                foreach (JObject change in InventoryChanges.Compare(before, after))
                    InventoryEventDeltas.Add(difference, change, (long)change["quantityDelta"]);
            var changes = InventoryEventDeltas.Read(after, difference);
            bool matches = before == null ? Fits(changes, difference)
                : changes.Count > 0 && changes.Count == difference.Count
                    && changes.All(pair => difference.TryGetValue(pair.Key, out long delta) && delta == pair.Value);
            if (!matches) return;
            result.Add(Verified(after, before, after, before == null
                ? "first_report_changes_present" : "client_changes_match_inventory_difference"));
            // Server observations follow the client report on this connection. They corroborate the
            // same change, with a separate quantity budget, rather than consuming the next interval.
            var remaining = new Dictionary<string, long>(changes);
            foreach (var entry in events.Where(e => SameCharacter(e, after) && e["streamId"] == null && !(e["items"] is JArray)
                && Time(e) >= Time(after) && (next == null || Time(e) < Time(next))))
            {
                var delta = InventoryEventDeltas.Read(entry, remaining);
                if (!Fits(delta, remaining)) continue;
                Consume(delta, remaining);
                result.Add(Verified(entry, before, after, "server_observation_matches_client_change"));
            }
        }

        private static Dictionary<string, long> Stock(JObject report)
        {
            var stock = new Dictionary<string, long>();
            foreach (JObject item in (JArray)report["items"])
                InventoryEventDeltas.Add(stock, item, (long)item["quantity"]);
            return stock;
        }

        private static bool Fits(Dictionary<string, long> changes, Dictionary<string, long> remaining)
            => changes.Count > 0 && changes.All(pair => remaining.TryGetValue(pair.Key, out long available)
                && Math.Sign(available) == Math.Sign(pair.Value) && Math.Abs(pair.Value) <= Math.Abs(available));

        private static void Consume(Dictionary<string, long> changes, Dictionary<string, long> remaining)
        {
            foreach (var pair in changes) remaining[pair.Key] -= pair.Value;
        }

        private static DateTime Time(JObject entry) => entry["utc"].Value<DateTime>();
        private static bool SameCharacter(JObject first, JObject second)
            => JToken.DeepEquals(first["characterId"], second["characterId"]);

        private static JObject Verified(JObject entry, JObject before, JObject after, string basis)
        {
            var copy = (JObject)entry.DeepClone();
            copy["verification"] = new JObject { ["fromEventId"] = before?["eventId"]?.DeepClone(),
                ["toEventId"] = after["eventId"].DeepClone(), ["verifiedUtc"] = DateTime.UtcNow,
                ["basis"] = basis };
            return copy;
        }
    }
}
