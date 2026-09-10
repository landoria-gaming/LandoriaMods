using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class CraftSignalMatcher
    {
        private sealed class Pair
        {
            internal JObject Start;
            internal JObject End;
            internal JObject Craft;
        }

        internal static void Match(List<JObject> events, List<JObject> verified)
        {
            var known = verified.Concat(events).GroupBy(e => (string)e["eventId"])
                .Select(group => group.First()).ToList();
            var usedCrafts = new HashSet<string>(known.Select(e => (string)e["verification"]?["craftEventId"])
                .Where(id => id != null));
            var proposals = new List<Pair>();
            foreach (var group in known.Where(IsSignal).GroupBy(e => (string)e["characterId"]))
            {
                var signals = group.OrderBy(Time).ToList();
                for (int i = 1; i < signals.Count; i++)
                {
                    var pair = Candidate(signals[i - 1], signals[i], known, usedCrafts);
                    if (pair != null) proposals.Add(pair);
                }
            }
            // A client craft cannot explain two signal pairs, even when their time windows overlap.
            foreach (var group in proposals.GroupBy(p => (string)p.Craft["eventId"]).Where(g => g.Count() == 1))
            {
                var pair = group.First();
                verified.Add(Link(pair.Start, pair.End, pair.Craft));
                verified.Add(Link(pair.End, pair.Start, pair.Craft));
            }
        }

        private static Pair Candidate(JObject start, JObject end, List<JObject> known, HashSet<string> used)
        {
            if ((string)start["event"] != "craft_started" || (string)end["event"] != "craft_ended"
                || start["verification"] != null || end["verification"] != null
                || Time(end) < Time(start) || Time(end) - Time(start) > TimeSpan.FromSeconds(30)) return null;
            // Compare server receipt times; allow a short delay between the sound and client RPC.
            var crafts = known.Where(e => (string)e["event"] == "item_crafted"
                && JToken.DeepEquals(e["characterId"], start["characterId"])
                && Time(e) >= Time(start) && Math.Abs((Time(e) - Time(end)).TotalSeconds) <= 2).ToList();
            if (crafts.Count != 1 || used.Contains((string)crafts[0]["eventId"])
                || crafts[0]["verification"] == null) return null;
            return new Pair { Start = start, End = end, Craft = crafts[0] };
        }

        private static JObject Link(JObject signal, JObject other, JObject craft)
        {
            var result = (JObject)signal.DeepClone();
            var verification = (JObject)craft["verification"].DeepClone();
            verification["basis"] = "craft_signals_match_verified_client_craft";
            verification["craftEventId"] = craft["eventId"].DeepClone();
            verification["pairedSignalEventId"] = other["eventId"].DeepClone();
            verification["verifiedUtc"] = DateTime.UtcNow;
            result["verification"] = verification;
            return result;
        }

        private static bool IsSignal(JObject entry)
            => (string)entry["event"] == "craft_started" || (string)entry["event"] == "craft_ended";
        private static DateTime Time(JObject entry) => entry["utc"].Value<DateTime>();
    }
}
