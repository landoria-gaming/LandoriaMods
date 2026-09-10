using System;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch]
    internal static class DropCursor
    {
        private static InventoryGui gui;
        private static ItemDrop.ItemData dragged;
        private static Inventory source;
        private static int amount;
        private static long revision;

        [HarmonyPostfix, HarmonyPatch(typeof(InventoryGui), "SetupDragItem")]
        private static void Track(InventoryGui __instance, ItemDrop.ItemData item, Inventory inventory, int amount)
        {
            gui = __instance;
            dragged = item;
            source = inventory;
            DropCursor.amount = amount;
            revision++;
        }

        internal static Action Completion(ItemDrop.ItemData item, Inventory inventory, int count)
        {
            if (gui == null || !ReferenceEquals(dragged, item) || source != inventory || amount != count) return null;
            long expected = revision;
            var expectedGui = gui;
            return () =>
            {
                if (expectedGui == null || gui != expectedGui || revision != expected) return;
                try { Clear(expectedGui, null, null, 1); }
                catch (Exception error) { CharacterRpc.Log.LogError(error); }
            };
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(InventoryGui), "SetupDragItem")]
        private static void Clear(InventoryGui instance, ItemDrop.ItemData item, Inventory inventory, int amount)
            => throw new NotImplementedException("Native drag cleanup was not patched.");
    }
}
