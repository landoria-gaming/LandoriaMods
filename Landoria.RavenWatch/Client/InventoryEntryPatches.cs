using System;
using HarmonyLib;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData) })]
    internal static class InventoryEntryAddPatch
    {
        private static void Prefix(global::Inventory __instance, ItemDrop.ItemData item, out InventoryEntryDates.State __state)
            => __state = InventoryEntryDates.Before(__instance, item);
        private static void Postfix(global::Inventory __instance, InventoryEntryDates.State __state)
            => InventoryEntryDates.After(__instance, __state);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(Vector2i) })]
    internal static class InventoryEntryPositionPatch
    {
        private static void Prefix(global::Inventory __instance, ItemDrop.ItemData item, out InventoryEntryDates.State __state)
            => __state = InventoryEntryDates.Before(__instance, item);
        private static void Postfix(global::Inventory __instance, InventoryEntryDates.State __state)
            => InventoryEntryDates.After(__instance, __state);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[]
        { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    internal static class InventoryEntrySlotPatch
    {
        private static void Prefix(global::Inventory __instance, ItemDrop.ItemData item, out InventoryEntryDates.State __state)
            => __state = InventoryEntryDates.Before(__instance, item);
        private static void Postfix(global::Inventory __instance, InventoryEntryDates.State __state)
            => InventoryEntryDates.After(__instance, __state);
    }

    [HarmonyPatch(typeof(global::Inventory), "Load", new Type[] { typeof(ZPackage) })]
    internal static class InventoryEntryLoadPatch
    {
        private static void Prefix(out int __state)
        { __state = InventoryEntryDates.Suppressed; InventoryEntryDates.Suppressed++; }
        private static Exception Finalizer(Exception __exception, int __state)
        { InventoryEntryDates.Suppressed = __state; return __exception; }
    }

    [HarmonyPatch(typeof(global::Inventory), "Load", new Type[] { typeof(ZPackage), typeof(bool) })]
    internal static class InventoryEntryTemporaryLoadPatch
    {
        private static void Prefix(out int __state)
        { __state = InventoryEntryDates.Suppressed; InventoryEntryDates.Suppressed++; }
        private static Exception Finalizer(Exception __exception, int __state)
        { InventoryEntryDates.Suppressed = __state; return __exception; }
    }

    [HarmonyPatch(typeof(global::Inventory), "MoveItemToThis", new Type[]
        { typeof(global::Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    internal static class InventoryEntryMovePatch
    {
        private static void Prefix(global::Inventory __instance, global::Inventory fromInventory, out int __state)
        {
            __state = InventoryEntryDates.InternalMove;
            if (__instance == fromInventory) InventoryEntryDates.InternalMove++;
        }
        private static Exception Finalizer(Exception __exception, int __state)
        { InventoryEntryDates.InternalMove = __state; return __exception; }
    }
}
