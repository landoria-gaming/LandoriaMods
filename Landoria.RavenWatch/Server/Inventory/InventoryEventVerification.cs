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
        internal const string PendingFile = "inventory-events-pending.json";
        internal const string UnverifiedFile = "inventory-events-unverified.json";
        private const string VerifiedFile = "inventory-events-verified.json";
        private const string TransactionFile = ".inventory-events-review.json";

        internal static string PreparePending(string directory)
        {
            Recover(directory);
            var pending = Read(directory, PendingFile);
            var rejected = Read(directory, UnverifiedFile);
            var legacy = Directory.EnumerateFiles(directory, "*inventory-events*.json")
                .Where(path => IsLegacyName(Path.GetFileName(path))).ToList();
            var migrated = rejected.Where(e => (int?)e["verificationAttempts"] < 3
                || e["verificationAttempts"] == null).ToList();
            foreach (var entry in migrated) { entry.Remove(); pending.Add(entry); }
            foreach (string path in legacy)
                foreach (var entry in JArray.Parse(File.ReadAllText(path))) pending.Add(entry);
            Commit(directory, pending, Read(directory, VerifiedFile), rejected);
            foreach (string path in legacy) File.Delete(path);
            return Path.Combine(directory, PendingFile);
        }

        private static bool IsLegacyName(string name)
        {
            if (name == "_inventory-events.json" || name == "inventory-events.json") return true;
            const string prefix = "inventory-events-";
            return name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal)
                && name.Length == prefix.Length + 8 + 5
                && name.Substring(prefix.Length, 8).All(char.IsDigit);
        }

        internal static void Review(string directory, Action<JObject> onUnverified = null)
        {
            Recover(directory);
            var archive = Read(directory, VerifiedFile);
            var rejected = Read(directory, UnverifiedFile);
            var finished = new HashSet<string>(archive.Concat(rejected).Select(e => (string)e["eventId"]));
            var pending = Unique(Read(directory, PendingFile).Where(e => !finished.Contains((string)e["eventId"])));
            foreach (var snapshot in pending.OfType<JObject>().Where(e => e["items"] is JArray).ToList())
            {
                var candidates = pending.TakeWhile(e => e != snapshot).OfType<JObject>()
                    .Where(e => e["items"] == null && JToken.DeepEquals(e["characterId"], snapshot["characterId"])).ToList();
                InventoryPendingMatcher.Match(candidates, archive, snapshot);
                int rejectedBefore = rejected.Count;
                Finish(candidates, snapshot, archive, rejected);
                snapshot["verification"] = new JObject { ["basis"] = "inventory_snapshot_recorded",
                    ["verifiedUtc"] = DateTime.UtcNow };
                snapshot.Remove();
                archive.Add(snapshot);
                Commit(directory, pending, archive, rejected);
                foreach (var entry in rejected.Skip(rejectedBefore).OfType<JObject>()) onUnverified?.Invoke(entry);
            }
        }

        private static void Finish(List<JObject> candidates, JObject snapshot,
            JArray archive, JArray rejected)
        {
            foreach (var entry in candidates)
            {
                if (entry["verification"] != null) { entry.Remove(); archive.Add(entry); continue; }
                if ((string)entry["lastAttemptInventoryId"] == (string)snapshot["eventId"]) continue;
                entry["lastAttemptInventoryId"] = snapshot["eventId"].DeepClone();
                int attempts = ((int?)entry["verificationAttempts"] ?? 0) + 1;
                entry["verificationAttempts"] = attempts;
                if (attempts < 3) continue;
                entry["verificationStatus"] = "unverified";
                entry.Remove();
                rejected.Add(entry);
            }
        }

        private static JArray Read(string directory, string name)
            => File.Exists(Path.Combine(directory, name))
                ? JArray.Parse(File.ReadAllText(Path.Combine(directory, name))) : new JArray();
        private static JArray Unique(IEnumerable<JToken> entries)
            => new JArray(entries.GroupBy(e => (string)e["eventId"]).Select(g => g.First()));

        private static void Commit(string directory, JArray pending, JArray archive, JArray rejected)
        {
            var complete = Unique(archive);
            var ids = new HashSet<string>(complete.Select(e => (string)e["eventId"]));
            var failed = Unique(rejected.Where(e => !ids.Contains((string)e["eventId"])));
            ids.UnionWith(failed.Select(e => (string)e["eventId"]));
            var state = new JObject { [PendingFile] = Unique(pending.Where(e => !ids.Contains((string)e["eventId"]))),
                [VerifiedFile] = complete, [UnverifiedFile] = failed };
            Write(Path.Combine(directory, TransactionFile), state);
            Recover(directory);
        }

        internal static void Recover(string directory)
        {
            string path = Path.Combine(directory, TransactionFile);
            if (!File.Exists(path)) return;
            var state = JObject.Parse(File.ReadAllText(path));
            foreach (string name in new[] { PendingFile, VerifiedFile, UnverifiedFile })
                Write(Path.Combine(directory, name), state[name]);
            File.Delete(path);
        }

        private static void Write(string path, JToken entries)
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
