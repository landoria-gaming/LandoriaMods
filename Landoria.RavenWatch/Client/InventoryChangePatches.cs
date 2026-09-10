using System;
using HarmonyLib;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(UnityEngine.GameObject), typeof(int) })]
    internal static class InventoryChangePatch0
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("AddItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData) })]
    internal static class InventoryChangePatch1
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("AddItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(Vector2i) })]
    internal static class InventoryChangePatch2
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("AddItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    internal static class InventoryChangePatch3
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("AddItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(bool), typeof(bool) })]
    internal static class InventoryChangePatch4
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("AddItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "AddItem", new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool) })]
    internal static class InventoryChangePatch5
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("AddItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "MoveAll", new Type[] { typeof(global::Inventory) })]
    internal static class InventoryChangePatch6
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("MoveAll");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "StackAll", new Type[] { typeof(global::Inventory), typeof(bool) })]
    internal static class InventoryChangePatch7
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("StackAll");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "MoveItemToThis", new Type[] { typeof(global::Inventory), typeof(ItemDrop.ItemData) })]
    internal static class InventoryChangePatch8
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("MoveItemToThis");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "MoveItemToThis", new Type[] { typeof(global::Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    internal static class InventoryChangePatch9
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("MoveItemToThis");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveItem", new Type[] { typeof(int) })]
    internal static class InventoryChangePatch10
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveOneItem", new Type[] { typeof(ItemDrop.ItemData) })]
    internal static class InventoryChangePatch11
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveOneItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveItem", new Type[] { typeof(ItemDrop.ItemData) })]
    internal static class InventoryChangePatch12
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int) })]
    internal static class InventoryChangePatch13
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveItem", new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
    internal static class InventoryChangePatch14
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveItem");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "MoveInventoryToGrave", new Type[] { typeof(global::Inventory) })]
    internal static class InventoryChangePatch15
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("MoveInventoryToGrave");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveAll", new Type[] {  })]
    internal static class InventoryChangePatch16
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveAll");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

    [HarmonyPatch(typeof(global::Inventory), "RemoveUnequipped", new Type[] {  })]
    internal static class InventoryChangePatch17
    {
        private static void Prefix(out InventoryChangeScope __state)
            => __state = InventoryChangeScope.Begin("RemoveUnequipped");
        private static Exception Finalizer(InventoryChangeScope __state, Exception __exception)
            => InventoryChangeScope.End(__state, __exception);
    }

}
