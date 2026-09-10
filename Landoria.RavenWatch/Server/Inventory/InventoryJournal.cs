using Landoria.RavenWatch.Shared;
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
        private sealed class CharacterJournal
        {
            internal RpcJournal Journal;
            internal string Directory;
            internal string Path;
            internal void Review()
            {
                Journal.Dispose();
                try { InventoryEventVerification.Review(Directory, Network.InventoryUnverifiedBroadcast.Send); }
                catch (Exception error) { RavenWatchLog.Log.LogError(error); }
                finally
                {
                    InventoryEventVerification.Recover(Directory);
                    Journal = RpcJournal.OpenPersistent(Path);
                }
            }

            internal void Append(JObject entry)
            {
                // Use the server observation time consistently for every inventory event.
                entry["eventDate"] = entry["utc"]?.Type == JTokenType.Date
                    ? new JValue(entry["utc"].Value<DateTime>().ToUniversalTime())
                    : entry["utc"]?.DeepClone() ?? new JValue(DateTime.UtcNow);
                if (entry["items"] == null) entry["verificationAttempts"] = 0;
                Journal.Append(entry);
            }
        }

        private readonly Dictionary<string, CharacterJournal> journals =
            new Dictionary<string, CharacterJournal>(StringComparer.OrdinalIgnoreCase);
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

        internal void CraftSound(JToken utc, ZNetPeer peer, ZDO sound, string signal)
        {
            var entry = CharacterIdentity.Add(new JObject
            {
                ["eventId"] = Guid.NewGuid().ToString("D"),
                ["utc"] = utc.DeepClone(),
                ["event"] = signal == "sfx_gui_craftitem" ? "craft_started" : "craft_ended",
                ["signal"] = signal, ["sourceZdo"] = sound.m_uid.ToString(),
                ["withoutStation"] = true, ["inferred"] = true, ["quantityDelta"] = 0
            }, peer);
            EventLocation.Add(entry, sound.GetPosition());
            var character = ZDOMan.instance?.GetZDO(peer.m_characterID);
            var journal = Journal(peer, entry, character?.GetPosition());
            journal.Append(entry);
        }

        internal void ClientInventory(ZNetPeer peer, JObject snapshot)
        {
            var entry = CharacterIdentity.Add((JObject)snapshot.DeepClone(), peer);
            if ((string)entry["context"] == "item_crafted") CraftMaterials.Add(entry);
            if (entry["eventId"] == null) entry["eventId"] = Guid.NewGuid().ToString("D");
            entry["utc"] = DateTime.UtcNow;
            entry["event"] = entry["context"].DeepClone();
            entry["inventorySource"] = "client_reported";
            // Item changes are detailed in the report context, not a single top-level delta.
            entry["quantityDelta"] = 0;
            var character = ZDOMan.instance?.GetZDO(peer.m_characterID);
            EventLocation.Add(entry, character?.GetPosition());
            var journal = Journal(peer, entry, character?.GetPosition());
            journal.Append(entry);
            if (entry["items"] is JArray)
            {
                ReceivedInventoryStore.Save(journal.Directory, entry);
                journal.Review();
            }
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

        private CharacterJournal Journal(ZNetPeer peer, JObject identity, Vector3? characterPosition)
        {
            string playerName = (string)identity["playerName"];
            string account = peer?.m_socket?.GetHostName();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(playerName))
                throw new InvalidDataException("Missing account or character name for inventory journal.");
            string folder = SafeName(account + "_" + playerName);
            if (journals.TryGetValue(folder, out var current)) return current;
            string playerDirectory = Path.Combine(directory, folder);
            Directory.CreateDirectory(playerDirectory);
            // Legacy journals also count as history before their filenames are consolidated.
            bool isNew = !Directory.EnumerateFiles(playerDirectory, "inventory-events*.json").Any()
                && !Directory.EnumerateFiles(playerDirectory, "_inventory-events*.json").Any();
            string path = InventoryEventVerification.PreparePending(playerDirectory);
            var journal = new CharacterJournal { Directory = playerDirectory, Path = path,
                Journal = RpcJournal.OpenPersistent(path) };
            journals.Add(folder, journal);
            if (isNew) AddStartingItems(journal, identity, characterPosition);
            return journal;
        }

        private static void AddStartingItems(CharacterJournal journal, JObject identity, Vector3? position)
        {
            // Current policy: a missing journal means a new character with starting equipment.
            foreach (JObject item in StartingInventory.Create())
            {
                item.Remove("quantity");
                item["eventId"] = Guid.NewGuid().ToString("D");
                item["utc"] = identity["utc"]?.DeepClone();
                item["event"] = "pickup";
                item["startingItem"] = true;
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
                catch (Exception error) { RavenWatchLog.Log.LogError(error); }
            }
            journals.Clear();
        }
    }
}
