using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server
{
    internal sealed class InventoryJournal : IDisposable
    {
        private readonly string directory;
        private readonly Dictionary<string, RpcJournal> journals =
            new Dictionary<string, RpcJournal>(StringComparer.OrdinalIgnoreCase);
        internal InventoryJournal(string directory) { this.directory = directory; }

        internal void Append(JToken utc, ZNetPeer peer, JObject item, string action, int delta,
            string prefab, Vector3 position)
        {
            if (delta == 0) return;
            var entry = CharacterIdentity.Add(new JObject
            {
                ["utc"] = utc?.DeepClone(), ["event"] = action,
                ["prefabHash"] = item["prefabHash"]?.DeepClone(), ["prefabName"] = prefab,
                ["quality"] = item["quality"]?.DeepClone(), ["variant"] = item["variant"]?.DeepClone(),
                ["worldLevel"] = item["worldLevel"]?.DeepClone(), ["quantityDelta"] = delta
            }, peer);
            EventLocation.Add(entry, position);
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

        private RpcJournal Journal(ZNetPeer peer, JObject identity, Vector3? characterPosition)
        {
            string playerName = (string)identity["playerName"];
            string account = peer?.m_socket?.GetHostName();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(playerName))
                throw new InvalidDataException("Missing account or character name for inventory journal.");
            string folder = SafeName(account + "_" + playerName);
            if (!journals.TryGetValue(folder, out var journal))
            {
                string path = Path.Combine(directory, folder, "_inventory-events.json");
                bool isNew = !File.Exists(path);
                journal = RpcJournal.OpenPersistent(path);
                journals.Add(folder, journal);
                if (isNew) AddStartingItems(journal, identity, characterPosition);
            }
            return journal;
        }

        private static void AddStartingItems(RpcJournal journal, JObject identity, Vector3? position)
        {
            // Current policy: a missing journal means a new character with starting equipment.
            foreach (JObject item in StartingInventory.Create())
            {
                item.Remove("quantity");
                item["utc"] = identity["utc"]?.DeepClone();
                item["event"] = "pickup";
                item["characterId"] = identity["characterId"]?.DeepClone();
                item["playerName"] = identity["playerName"]?.DeepClone();
                item["quantityDelta"] = 1;
                EventLocation.Add(item, position);
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
                try { journal.Dispose(); }
                catch (Exception error) { RpcCapture.Log.LogError(error); }
            }
            journals.Clear();
        }
    }
}
