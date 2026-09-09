using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class InventoryCapture
    {
        internal sealed class ReceiveScope
        {
            internal ZNetPeer Peer;
            internal JToken Utc;
            internal readonly HashSet<ZDOID> Created = new HashSet<ZDOID>();
        }

        [ThreadStatic] internal static ReceiveScope Current;
        private static InventoryJournal journal;
        private static InventoryJournal Journal => journal ?? (journal = new InventoryJournal(
            Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "RavenWatch")));

        internal static ReceiveScope Begin(ZRpc rpc)
        {
            var previous = Current;
            var peer = ZNet.instance.GetPeers().FirstOrDefault(p => p.m_rpc == rpc);
            Current = peer != null && peer.IsReady()
                ? new ReceiveScope { Peer = peer, Utc = new JValue(DateTime.UtcNow) } : null;
            return previous;
        }

        internal static void Received(ZDO zdo)
        {
            var scope = Current;
            if (scope == null) return;
            bool created = scope.Created.Remove(zdo.m_uid);
            if (zdo.m_uid.UserID != scope.Peer.m_uid || zdo.GetOwner() != scope.Peer.m_uid) return;
            var prefab = ZNetScene.instance?.GetPrefab(zdo.GetPrefab());
            if (prefab == null) return;
            if (prefab.GetComponent<Player>() != null)
            {
                long id = zdo.GetLong(ZDOVars.s_playerID, 0L);
                if (id != 0) Journal.EnsureCharacter(scope.Utc, scope.Peer, id,
                    zdo.GetString(ZDOVars.s_playerName, scope.Peer.m_playerName), zdo.GetPosition());
            }
            else if (created && prefab.GetComponent<ItemDrop>() != null)
            {
                var item = ReadItem(zdo);
                // Starting equipment has never been picked up, even when the player drops it.
                if (item != null && item.m_stack > 0
                    && (item.m_pickedUp || StartingInventory.Matches(zdo.GetPrefab(), item)))
                    Write(scope.Utc, scope.Peer, zdo, item, "drop", -item.m_stack, prefab.name);
            }
        }

        internal static void Destroyed(long sender, ZPackage package)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(p => p.m_uid == sender);
            if (peer == null || !peer.IsReady()) return;
            // Read an independent package so the vanilla handler retains its original read position.
            var copy = new ZPackage(package.GetArray());
            copy.SetPos(package.GetPos());
            int count = copy.ReadInt();
            if (count < 0 || count > (copy.Size() - copy.GetPos()) / 12)
                throw new InvalidDataException("Invalid DestroyZDO count.");
            var seen = new HashSet<ZDOID>();
            var utc = new JValue(DateTime.UtcNow);
            for (int i = 0; i < count; i++)
            {
                var id = copy.ReadZDOID();
                if (!seen.Add(id)) continue;
                var zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null) continue;
                var prefab = ZNetScene.instance?.GetPrefab(zdo.GetPrefab());
                if (prefab == null || prefab.GetComponent<ItemDrop>() == null) continue;
                var item = ReadItem(zdo);
                // Current policy counts ItemDrop destruction as pickup by the sending character.
                if (item != null && item.m_stack > 0)
                    Write(utc, peer, zdo, item, "pickup", item.m_stack, prefab.name);
            }
        }

        private static ItemDrop.ItemData ReadItem(ZDO zdo)
        {
            var bytes = zdo.GetByteArray(ZDOVars.s_itemData);
            if (bytes == null) return null;
            var package = new ZPackage(bytes);
            var version = (global::Version.Item)package.ReadByte();
            if (version != global::Version.Item.ChunksNCheats)
                throw new InvalidDataException("Unsupported inventory item version: " + version);
            var result = ItemDrop.ItemData.Load(package, version);
            if (package.GetPos() != package.Size())
                throw new InvalidDataException("Trailing inventory item bytes.");
            return result.itemData;
        }

        internal static void Write(JToken utc, ZNetPeer peer, ZDO zdo, ItemDrop.ItemData item,
            string action, int delta, string prefab, int? itemPrefabHash = null)
        {
            var fields = new JObject { ["prefabHash"] = itemPrefabHash ?? zdo.GetPrefab(), ["quality"] = item.m_quality,
                ["variant"] = item.m_variant, ["worldLevel"] = item.m_worldLevel };
            Journal.Append(utc, peer, fields, action, delta, prefab, zdo.GetPosition());
        }

        internal static void Close()
        {
            try { journal?.Dispose(); }
            finally { journal = null; Current = null; }
        }
    }
}
