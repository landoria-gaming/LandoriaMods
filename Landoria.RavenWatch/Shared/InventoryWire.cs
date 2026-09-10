using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Shared
{
    internal static class InventoryWire
    {
        internal const int MaximumBytes = 2 * 1024 * 1024;
        internal const int MaximumItems = 1024;



        internal static JObject Read(ZPackage package, long characterId)
        {
            if (package.Size() > MaximumBytes)
                throw new InvalidDataException("Inventory exceeds the byte limit.");
            var captured = new DateTime(package.ReadLong(), DateTimeKind.Utc);
            var version = (global::Version.Item)package.ReadInt();
            if (version != global::Version.Item.ChunksNCheats)
                throw new InvalidDataException("Unsupported client inventory version: " + version);
            int count = package.ReadUShort();
            if (count > MaximumItems) throw new InvalidDataException("Inventory exceeds the item limit.");
            var items = new JArray();
            var slots = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                var loaded = ItemDrop.ItemData.Load(package, version);
                var item = loaded.itemData;
                if (item.m_stack <= 0 || item.m_quality <= 0
                    || !slots.Add(item.m_gridPos.x + ":" + item.m_gridPos.y))
                    throw new InvalidDataException("Invalid client inventory stack or duplicate slot.");
                items.Add(Entry(loaded.prefabHash, item, characterId));
            }
            if (package.GetPos() != package.Size())
                throw new InvalidDataException("Trailing client inventory bytes.");
            return new JObject { ["status"] = "received", ["clientCapturedUtc"] = captured,
                ["items"] = items };
        }

        private static JObject Entry(int hash, ItemDrop.ItemData item, long characterId)
            => new JObject
            {
                ["characterId"] = characterId.ToString(CultureInfo.InvariantCulture),
                ["prefabHash"] = hash, ["prefabName"] = ZNetScene.instance?.GetPrefab(hash)?.name,
                ["quantity"] = item.m_stack, ["quality"] = item.m_quality, ["variant"] = item.m_variant,
                ["worldLevel"] = item.m_worldLevel, ["durability"] = item.m_durability,
                ["slotX"] = item.m_gridPos.x, ["slotY"] = item.m_gridPos.y,
                ["equipped"] = item.m_equipped, ["pickedUp"] = item.m_pickedUp,
                ["cheated"] = item.m_cheated,
                ["crafterId"] = item.m_crafterID.ToString(CultureInfo.InvariantCulture),
                ["crafterName"] = item.m_crafterName, ["customData"] = JObject.FromObject(item.m_customData)
            };
    }
}
