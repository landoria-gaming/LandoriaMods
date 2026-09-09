using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class TombstoneCapture
    {
        private sealed class Stock
        {
            internal int Hash;
            internal ItemDrop.ItemData Item;
            internal int Quantity;
        }

        internal static byte[] Before(ZDO zdo)
        {
            if (InventoryCapture.Current == null) return null;
            var prefab = ZNetScene.instance?.GetPrefab(zdo.GetPrefab());
            if (prefab == null || prefab.GetComponent<TombStone>() == null) return null;
            // Deserialize replaces byte arrays; keep the old reference only for this call.
            return zdo.GetByteArray(ZDOVars.s_items);
        }

        internal static void After(ZDO zdo, byte[] before)
        {
            var scope = InventoryCapture.Current;
            if (scope == null || before == null || zdo.GetOwner() != scope.Peer.m_uid) return;
            var after = zdo.GetByteArray(ZDOVars.s_items);
            if (after == null || before.SequenceEqual(after)) return;
            var previous = Read(before);
            var current = Read(after);
            foreach (var pair in previous)
            {
                int remaining = current.TryGetValue(pair.Key, out var stock) ? stock.Quantity : 0;
                int removed = pair.Value.Quantity - remaining;
                if (removed <= 0) continue;
                var prefab = ZNetScene.instance.GetPrefab(pair.Value.Hash);
                if (prefab == null)
                    throw new InvalidDataException("Unknown tombstone item prefab: " + pair.Value.Hash);
                // Attribute withdrawals to the client sending the accepted container update.
                InventoryCapture.Write(scope.Utc, scope.Peer, zdo, pair.Value.Item,
                    "pickup", removed, prefab.name, pair.Value.Hash);
            }
        }

        private static Dictionary<string, Stock> Read(byte[] bytes)
        {
            var package = new ZPackage(bytes);
            var version = (global::Version.Item)package.ReadInt();
            if (version != global::Version.Item.ChunksNCheats)
                throw new InvalidDataException("Unsupported tombstone inventory version: " + version);
            int count = package.ReadUShort();
            var result = new Dictionary<string, Stock>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                var entry = ItemDrop.ItemData.Load(package, version);
                var item = entry.itemData;
                if (item.m_stack <= 0) throw new InvalidDataException("Invalid tombstone item quantity.");
                string key = entry.prefabHash + ":" + item.m_quality + ":" + item.m_variant + ":" + item.m_worldLevel;
                if (!result.TryGetValue(key, out var stock))
                    result.Add(key, stock = new Stock { Hash = entry.prefabHash, Item = item });
                stock.Quantity = checked(stock.Quantity + item.m_stack);
            }
            if (package.GetPos() != package.Size())
                throw new InvalidDataException("Trailing tombstone inventory bytes.");
            return result;
        }
    }
}
