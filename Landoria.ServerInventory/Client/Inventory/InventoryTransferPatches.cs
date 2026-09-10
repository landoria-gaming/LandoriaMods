using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData))]
    internal static class MoveWholeItemPatch
    {
        private static bool Prefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item)
            => !InventoryActionRequest.Transfer(__instance, fromInventory, item, item.m_stack, "item");
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    internal static class MovePartialItemPatch
    {
        private static bool Prefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item, int amount, ref bool __result)
        {
            if (!InventoryActionRequest.Transfer(__instance, fromInventory, item, amount, "item")) return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
    internal static class MoveAllItemsPatch
    {
        private static bool Prefix(Inventory __instance, Inventory fromInventory)
            => !InventoryActionRequest.Transfer(__instance, fromInventory, null, 0, "all");
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
    internal static class StackAllItemsPatch
    {
        private static bool Prefix(Inventory __instance, Inventory fromInventory)
            => !InventoryActionRequest.Transfer(__instance, fromInventory, null, 0, "stack");
    }
}
