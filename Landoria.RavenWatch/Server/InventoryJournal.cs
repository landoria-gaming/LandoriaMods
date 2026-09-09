using System;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class InventoryJournal : IDisposable
    {
        private readonly RpcJournal journal;
        internal InventoryJournal(string directory)
            => journal = new RpcJournal(directory, "inventory-events");

        internal void Append(JToken utc, ZNetPeer peer, JObject item, string action, int delta,
            string prefab, long? characterId = null, string playerName = null)
        {
            if (delta == 0) return;
            var entry = CharacterIdentity.Add(new JObject
            {
                ["utc"] = utc?.DeepClone(), ["event"] = action,
                ["prefabHash"] = item["prefabHash"]?.DeepClone(), ["prefabName"] = prefab,
                ["quality"] = item["quality"]?.DeepClone(), ["variant"] = item["variant"]?.DeepClone(),
                ["worldLevel"] = item["worldLevel"]?.DeepClone(), ["quantityDelta"] = delta
            }, peer);
            if (characterId.HasValue)
            {
                entry["characterId"] = characterId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                entry["playerName"] = playerName;
            }
            journal.Append(entry);
        }

        public void Dispose() => journal.Dispose();
    }
}
