using System;
using System.Linq;

namespace Landoria.RavenWatch
{
    [Serializable]
    internal sealed class InventoryItemSnapshot
    {
        public string prefab;
        public string name;
        public string type;
        public int quantity;
        public int maximumStack;
        public int quality;
        public int variant;
        public int worldLevel;
        public float durability;
        public float maximumDurability;
        public float stackWeight;
        public bool equipped;
        public int column;
        public int row;
        public string crafterId;
        public string crafterName;
        public ItemCustomData[] customData;

        internal static InventoryItemSnapshot Capture(ItemDrop.ItemData item)
        {
            return new InventoryItemSnapshot
            {
                prefab = item.m_dropPrefab ? item.m_dropPrefab.name : null,
                name = item.m_shared.m_name, type = item.m_shared.m_itemType.ToString(),
                quantity = item.m_stack, maximumStack = item.m_shared.m_maxStackSize,
                quality = item.m_quality, variant = item.m_variant, worldLevel = item.m_worldLevel,
                durability = item.m_durability, maximumDurability = item.GetMaxDurability(),
                stackWeight = item.GetWeight(), equipped = item.m_equipped,
                column = item.m_gridPos.x, row = item.m_gridPos.y,
                crafterId = item.m_crafterID.ToString(), crafterName = item.m_crafterName,
                customData = item.m_customData.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new ItemCustomData { key = pair.Key, value = pair.Value }).ToArray()
            };
        }
    }

    [Serializable]
    internal sealed class ItemCustomData
    {
        public string key;
        public string value;
    }
}
