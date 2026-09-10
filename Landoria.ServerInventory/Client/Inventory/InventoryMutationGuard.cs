using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    internal static class InventoryMutationGuard
    {
        internal static bool Allowed(Inventory inventory)
            => !ClientDamageGuard.Active || ItemAdditionCapture.Loading > 0 || Player.m_localPlayer == null ||
                inventory != Player.m_localPlayer.GetInventory();
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
    internal static class LocalInventoryAddition
    {
        private static bool Prefix(Inventory __instance, ref bool __result)
        {
            if (InventoryMutationGuard.Allowed(__instance)) return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
    internal static class LocalInventoryPositionedAddition
    {
        private static bool Prefix(Inventory __instance, ref bool __result)
        {
            if (InventoryMutationGuard.Allowed(__instance)) return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool))]
    internal static class LocalInventoryStackAddition
    {
        private static bool Prefix(Inventory __instance, ref bool __result)
        {
            if (InventoryMutationGuard.Allowed(__instance)) return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int),
        typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
    internal static class LocalInventoryNamedAddition
    {
        private static bool Prefix(Inventory __instance, ref ItemDrop.ItemData __result)
        {
            if (InventoryMutationGuard.Allowed(__instance)) return true;
            __result = null; return false;
        }
    }
}
