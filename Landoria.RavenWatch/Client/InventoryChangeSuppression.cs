using System;
using HarmonyLib;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class InventoryChangeSuppression0
    {
        private static void Prefix() => InventoryChangeScope.Suppressed++;
        private static Exception Finalizer(Exception __exception)
        { InventoryChangeScope.Suppressed--; return __exception; }
    }
    [HarmonyPatch(typeof(Humanoid), "DrainEquipedItemDurability")]
    internal static class InventoryChangeSuppression1
    {
        private static void Prefix() => InventoryChangeScope.Suppressed++;
        private static Exception Finalizer(Exception __exception)
        { InventoryChangeScope.Suppressed--; return __exception; }
    }
    [HarmonyPatch(typeof(Player), "Load")]
    internal static class InventoryChangeSuppression2
    {
        private static void Prefix() => InventoryChangeScope.Suppressed++;
        private static Exception Finalizer(Exception __exception)
        { InventoryChangeScope.Suppressed--; return __exception; }
    }
}
