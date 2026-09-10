using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Landoria.ServerInventory.Network
{
    [HarmonyPatch]
    internal static class InventorySnapshot
    {
        internal static void Apply(Player player, ZPackage package)
        {
            var snapshot = new Inventory(true);
            snapshot.Load(package);
            var inventory = player.GetInventory();
            var available = inventory.GetAllItems().ToList();
            var incoming = snapshot.GetAllItems();
            var matches = new Dictionary<ItemDrop.ItemData, ItemDrop.ItemData>();
            // Reserve exact slot matches before matching items moved to another slot.
            foreach (var item in incoming)
                Match(item, available, matches, sameSlot: true);
            foreach (var item in incoming)
                if (!matches.ContainsKey(item)) Match(item, available, matches, sameSlot: false);
            foreach (var removed in available) player.UnequipItem(removed, false);
            var result = new List<ItemDrop.ItemData>();
            foreach (var item in incoming)
            {
                if (!matches.TryGetValue(item, out var current))
                {
                    item.m_equipped = false;
                    current = item;
                }
                else CopyState(current, item);
                if (current.m_shared.m_useDurability && current.m_durability <= 0f)
                    player.UnequipItem(current, false);
                result.Add(current);
            }
            inventory.GetAllItems().Clear();
            inventory.GetAllItems().AddRange(result);
            NotifyChanged(inventory, false, false);
        }

        private static void Match(ItemDrop.ItemData incoming, List<ItemDrop.ItemData> available,
            Dictionary<ItemDrop.ItemData, ItemDrop.ItemData> matches, bool sameSlot)
        {
            var current = available.FirstOrDefault(item => SameItem(item, incoming) &&
                (!sameSlot || item.m_gridPos.Equals(incoming.m_gridPos)));
            if (current == null) return;
            available.Remove(current);
            matches.Add(incoming, current);
        }

        private static bool SameItem(ItemDrop.ItemData left, ItemDrop.ItemData right)
            => left.m_dropPrefab == right.m_dropPrefab && left.m_quality == right.m_quality &&
                left.m_variant == right.m_variant && left.m_worldLevel == right.m_worldLevel &&
                left.m_crafterID == right.m_crafterID && left.m_crafterName == right.m_crafterName &&
                left.m_customData.Count == right.m_customData.Count && left.m_customData.All(pair =>
                    right.m_customData.TryGetValue(pair.Key, out var value) && value == pair.Value);

        private static void CopyState(ItemDrop.ItemData current, ItemDrop.ItemData incoming)
        {
            current.m_stack = incoming.m_stack;
            current.m_durability = incoming.m_durability;
            current.m_gridPos = incoming.m_gridPos;
            current.m_pickedUp = incoming.m_pickedUp;
            current.m_cheated = incoming.m_cheated;
            // Keep equipment and attack references attached to the same ItemData instance.
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Inventory), "Changed")]
        private static void NotifyChanged(Inventory instance, bool success, bool cheatedStateChanged)
            => throw new NotImplementedException("Native inventory notification was not patched.");
    }
}
