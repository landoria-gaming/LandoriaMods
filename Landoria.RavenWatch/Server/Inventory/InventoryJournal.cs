using Landoria.RavenWatch.Server.Journal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal sealed class InventoryJournal : IDisposable
    {
        private readonly string directory;
        private sealed class DailyJournal
        {
            internal string Date;
            internal RpcJournal Journal;
            internal string Directory;
            internal InventorySnapshot Snapshot;

            internal void Append(JObject entry)
            {
                Journal.Append(entry);
                try
                {
                    // Reload after a previous projection failure; the journal remains authoritative.
                    if (Snapshot == null) Snapshot = new InventorySnapshot(Directory);
                    else Snapshot.Apply(entry);
                    Snapshot.Save();
                }
                catch (Exception error)
                {
                    RpcCapture.Log.LogError(error);
                    Snapshot = null;
                    throw;
                }
            }
        }

        private readonly Dictionary<string, DailyJournal> journals =
            new Dictionary<string, DailyJournal>(StringComparer.OrdinalIgnoreCase);
        internal InventoryJournal(string directory) { this.directory = directory; }

        internal void Append(JToken utc, ZNetPeer peer, JObject item, string action, int delta,
            string prefab, Vector3 position)
        {
            if (delta == 0) return;
            var entry = CharacterIdentity.Add(new JObject
            {
                ["eventId"] = Guid.NewGuid().ToString("D"),
                ["utc"] = utc?.DeepClone(), ["event"] = action,
                ["prefabHash"] = item["prefabHash"]?.DeepClone(), ["prefabName"] = prefab,
                ["quality"] = item["quality"]?.DeepClone(), ["variant"] = item["variant"]?.DeepClone(),
                ["worldLevel"] = item["worldLevel"]?.DeepClone(), ["quantityDelta"] = delta
            }, peer);
            EventLocation.Add(entry, position);
            ItemProperties.Add(entry);
            var character = peer == null ? null : ZDOMan.instance?.GetZDO(peer.m_characterID);
            Journal(peer, entry, character?.GetPosition()).Append(entry);
        }

        internal void EnsureCharacter(JToken utc, ZNetPeer peer, long characterId, string playerName, Vector3 position)
        {
            Journal(peer, new JObject
            {
                ["utc"] = utc.DeepClone(),
                ["characterId"] = characterId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["playerName"] = playerName
            }, position);
        }

        private DailyJournal Journal(ZNetPeer peer, JObject identity, Vector3? characterPosition)
        {
            string playerName = (string)identity["playerName"];
            string account = peer?.m_socket?.GetHostName();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(playerName))
                throw new InvalidDataException("Missing account or character name for inventory journal.");
            string folder = SafeName(account + "_" + playerName);
            string date = identity["utc"].Value<DateTime>().ToLocalTime()
                .ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            if (journals.TryGetValue(folder, out var current))
            {
                if (current.Date == date) return current;
                current.Journal.Dispose();
                journals.Remove(folder);
            }
            string playerDirectory = Path.Combine(directory, folder);
            Directory.CreateDirectory(playerDirectory);
            // A new day is not a new character; the legacy filename also counts as history.
            bool isNew = !Directory.EnumerateFiles(playerDirectory, "inventory-events*.json").Any()
                && !Directory.EnumerateFiles(playerDirectory, "_inventory-events*.json").Any();
            string path = Path.Combine(playerDirectory, "inventory-events-" + date + ".json");
            var snapshot = new InventorySnapshot(playerDirectory);
            snapshot.Save();
            var journal = new DailyJournal { Date = date, Directory = playerDirectory,
                Journal = RpcJournal.OpenPersistent(path), Snapshot = snapshot };
            journals.Add(folder, journal);
            if (isNew) AddStartingItems(journal, identity, characterPosition);
            return journal;
        }

        private static void AddStartingItems(DailyJournal journal, JObject identity, Vector3? position)
        {
            // Current policy: a missing journal means a new character with starting equipment.
            foreach (JObject item in StartingInventory.Create())
            {
                item.Remove("quantity");
                item["eventId"] = Guid.NewGuid().ToString("D");
                item["utc"] = identity["utc"]?.DeepClone();
                item["event"] = "pickup";
                item["characterId"] = identity["characterId"]?.DeepClone();
                item["playerName"] = identity["playerName"]?.DeepClone();
                item["quantityDelta"] = 1;
                EventLocation.Add(item, position);
                ItemProperties.Add(item);
                journal.Append(item);
            }
        }

        private static string SafeName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => c < 32 || invalid.Contains(c) || c == '/' || c == '\\'
                ? '_' : c).ToArray()).TrimEnd(' ', '.');
        }

        public void Dispose()
        {
            foreach (var journal in journals.Values)
            {
                try { journal.Journal.Dispose(); }
                catch (Exception error) { RpcCapture.Log.LogError(error); }
            }
            journals.Clear();
        }
    }
}
