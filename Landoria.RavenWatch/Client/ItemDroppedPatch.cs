using System;
using HarmonyLib;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(Humanoid), "DropItem", new Type[] { typeof(global::Inventory), typeof(ItemDrop.ItemData), typeof(int) })]
    internal static class ItemDroppedPatch
    {
        private static void Prefix(Humanoid __instance, out InventoryChangeScope __state)
            => __state = __instance == Player.m_localPlayer ? InventoryChangeScope.Begin("DropItem") : null;
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => __state == null ? __exception : InventoryChangeScope.End(__state, __exception);
    }
}
