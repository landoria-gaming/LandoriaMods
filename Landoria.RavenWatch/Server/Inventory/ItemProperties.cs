using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class ItemProperties
    {
        internal static void Add(JObject entry)
        {
            var prefab = ZNetScene.instance?.GetPrefab((int)entry["prefabHash"]);
            var shared = prefab?.GetComponent<ItemDrop>()?.m_itemData.m_shared;
            if (shared == null)
            {
                entry["weight"] = null;
                entry["stackable"] = null;
                entry["maxStackSize"] = null;
                return;
            }
            // Use vanilla quality-dependent unit weight without modifying the prefab's item data.
            var item = new ItemDrop.ItemData { m_shared = shared, m_quality = (int)entry["quality"] };
            entry["weight"] = item.GetNonStackedWeight();
            entry["stackable"] = shared.m_maxStackSize > 1;
            entry["maxStackSize"] = shared.m_maxStackSize;
        }
    }
}
