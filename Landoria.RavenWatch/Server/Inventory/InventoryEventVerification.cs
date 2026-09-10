using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryEventVerification
    {
        internal const string UnverifiedFile = "inventory-events-unverified.json";

        internal static string PrepareUnverified(string directory)
        {
            string target = Path.Combine(directory, UnverifiedFile);
            var legacy = Directory.EnumerateFiles(directory, "*inventory-events*.json")
                .Where(path => IsLegacyName(Path.GetFileName(path))).OrderBy(path => path, StringComparer.Ordinal).ToList();
            if (legacy.Count == 0) return target;
            var paths = File.Exists(target) ? new[] { target }.Concat(legacy) : legacy;
            var entries = paths.SelectMany(path => JArray.Parse(File.ReadAllText(path)).Cast<JObject>())
                .GroupBy(e => (string)e["eventId"]).Select(group => group.First())
                .OrderBy(e => e["utc"].Value<DateTime>()).ToList();
            // Persist all entries first; retries deduplicate an interrupted rename by event ID.
            Write(target, new JArray(entries));
            foreach (string path in legacy) File.Delete(path);
            return target;
        }

        private static bool IsLegacyName(string name)
        {
            if (name == "_inventory-events.json" || name == "inventory-events.json") return true;
            const string prefix = "inventory-events-";
            return name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal)
                && name.Length == prefix.Length + 8 + 5
                && name.Substring(prefix.Length, 8).All(char.IsDigit);
        }

        internal static void Review(string directory)
        {
            string archivePath = Path.Combine(directory, "inventory-events-verified.json");
            var archive = File.Exists(archivePath) ? JArray.Parse(File.ReadAllText(archivePath)) : new JArray();
            var files = Directory.EnumerateFiles(directory, "*inventory-events*.json")
                .Where(path => path != archivePath).OrderBy(path => path, StringComparer.Ordinal).ToList();
            var journals = files.ToDictionary(path => path, path => JArray.Parse(File.ReadAllText(path)));
            var events = archive.Cast<JObject>()
                .Concat(journals.Values.SelectMany(array => array.Cast<JObject>())).GroupBy(e => (string)e["eventId"])
                .Select(group => group.First()).OrderBy(e => e["utc"].Value<DateTime>()).ToList();
            var snapshots = events.Where(e => (string)e["status"] == "received" && e["items"] is JArray).ToList();
            var verified = InventoryEventMatcher.Match(events, snapshots);
            var retained = new JArray(archive.Cast<JObject>().Where(e =>
                (string)e["verification"]?["basis"] != "consecutive_client_inventories")
                .GroupBy(e => (string)e["eventId"]).Select(group => group.First()));
            var retainedIds = new HashSet<string>(retained.Cast<JObject>().Select(e => (string)e["eventId"]));
            foreach (var entry in verified)
                if (retainedIds.Add((string)entry["eventId"])) retained.Add(entry);
            RestoreLegacyEvents(archive, retainedIds, journals, directory);
            // Commit the archive first; retries remove any remaining duplicate source IDs.
            if (retained.Count > 0 || File.Exists(archivePath)) Write(archivePath, retained);
            foreach (var pair in journals)
            {
                var remaining = new JArray(pair.Value.Cast<JObject>()
                    .Where(e => !retainedIds.Contains((string)e["eventId"])));
                if (remaining.Count != pair.Value.Count) Write(pair.Key, remaining);
            }
        }

        private static void RestoreLegacyEvents(JArray archive, HashSet<string> retainedIds,
            Dictionary<string, JArray> journals, string directory)
        {
            // Rebuild old matches with the corrected association, preserving all event payloads.
            foreach (var entry in archive.Cast<JObject>().Where(e => !retainedIds.Contains((string)e["eventId"])))
            {
                var copy = (JObject)entry.DeepClone();
                copy.Remove("verification");
                string path = Path.Combine(directory, UnverifiedFile);
                if (!journals.ContainsKey(path)) journals[path] = new JArray();
                if (!journals[path].Cast<JObject>().Any(e => (string)e["eventId"] == (string)copy["eventId"]))
                    journals[path].Add(copy);
                Write(path, journals[path]);
            }
        }

        private static void Write(string path, JArray entries)
        {
            string temporary = path + ".tmp";
            byte[] bytes = new UTF8Encoding(false).GetBytes(entries.ToString(Formatting.Indented));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
