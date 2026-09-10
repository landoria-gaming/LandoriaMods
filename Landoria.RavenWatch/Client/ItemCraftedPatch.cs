using System;
using System.Linq;
using HarmonyLib;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class ItemCraftedPatch
    {
        internal sealed class State
        {
            internal string Prefab;
            internal int Quality;
            internal int Variant;
            internal int Before;
            internal bool Upgrade;
            internal int CraftCount;
            internal bool Upgrader;
            internal bool Matches(ItemDrop.ItemData item) => item.m_dropPrefab != null
                && item.m_dropPrefab.name == Prefab && item.m_quality == Quality && item.m_variant == Variant;
        }

        private static void Prefix(Player player, Recipe ___m_craftRecipe,
            ItemDrop.ItemData ___m_craftUpgradeItem, int ___m_craftVariant,
            bool ___m_multiCrafting, int ___m_multiCraftAmount, out State __state)
        {
            __state = null;
            try
            {
                if (player != Player.m_localPlayer || ___m_craftRecipe == null) return;
                var state = new State { Prefab = ___m_craftRecipe.m_item.gameObject.name,
                    Quality = ___m_craftUpgradeItem == null ? 1 : ___m_craftUpgradeItem.m_quality + 1,
                    Variant = ___m_craftUpgradeItem?.m_variant ?? ___m_craftVariant,
                    Upgrade = ___m_craftUpgradeItem != null,
                    CraftCount = ___m_multiCrafting ? ___m_multiCraftAmount : 1,
                    Upgrader = player.GetCurrentCraftingStation()?.m_upgrader ?? false };
                state.Before = player.GetInventory().GetAllItems().Where(state.Matches).Sum(item => item.m_stack);
                __state = state;
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        private static void Postfix(Player player, State __state)
        {
            if (__state == null) return;
            try
            {
                var items = player.GetInventory().GetAllItems().Where(__state.Matches).ToList();
                int produced = items.Sum(item => item.m_stack) - __state.Before;
                if (produced > 0) ItemCraftedRpc.Send(player, items[0], produced, __state.Upgrade,
                    __state.CraftCount, __state.Upgrader);
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
