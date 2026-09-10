using System;
using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    internal sealed class NativeCraftInventory : IDisposable
    {
        [ThreadStatic] internal static NativeCraftInventory Current;
        internal readonly Player Player;
        internal readonly Inventory Inventory;
        private readonly NativeCraftInventory previous;

        internal NativeCraftInventory(Player player, Inventory inventory)
        {
            Player = player;
            Inventory = inventory;
            previous = Current;
            Current = this;
        }

        internal static Inventory Enter(Player player, ref Inventory inventory)
        {
            if (Current == null || Current.Player != player) return null;
            var previous = inventory;
            inventory = Current.Inventory;
            return previous;
        }

        public void Dispose() => Current = previous;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class CraftRequirementsInventoryPatch
    {
        private static void Prefix(Player __instance, ref Inventory ___m_inventory, out Inventory __state)
            => __state = NativeCraftInventory.Enter(__instance, ref ___m_inventory);

        private static Exception Finalizer(ref Inventory ___m_inventory, Inventory __state, Exception __exception)
        {
            if (__state != null) ___m_inventory = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    internal static class CraftConsumptionInventoryPatch
    {
        private static void Prefix(Player __instance, ref Inventory ___m_inventory, out Inventory __state)
            => __state = NativeCraftInventory.Enter(__instance, ref ___m_inventory);

        private static Exception Finalizer(ref Inventory ___m_inventory, Inventory __state, Exception __exception)
        {
            if (__state != null) ___m_inventory = __state;
            return __exception;
        }
    }
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    internal static class BuildRequirementsInventoryPatch
    {
        private static void Prefix(Player __instance, ref Inventory ___m_inventory, out Inventory __state)
            => __state = NativeCraftInventory.Enter(__instance, ref ___m_inventory);

        private static Exception Finalizer(ref Inventory ___m_inventory, Inventory __state, Exception __exception)
        {
            if (__state != null) ___m_inventory = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    internal static class TombstoneInventoryPatch
    {
        private static void Prefix(Player __instance, ref Inventory ___m_inventory, out Inventory __state)
            => __state = NativeCraftInventory.Enter(__instance, ref ___m_inventory);

        private static Exception Finalizer(ref Inventory ___m_inventory, Inventory __state, Exception __exception)
        {
            if (__state != null) ___m_inventory = __state;
            return __exception;
        }
    }
}
