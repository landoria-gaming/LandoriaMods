using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "Show")]
    internal static class InventoryGuiShowServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "Hide")]
    internal static class InventoryGuiHideServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "SetInventorySize")]
    internal static class InventoryGuiSetInventorySizeServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "OnOpenSkills")]
    internal static class InventoryGuiOnOpenSkillsServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "OnOpenTexts")]
    internal static class InventoryGuiOnOpenTextsServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "OnOpenTrophies")]
    internal static class InventoryGuiOnOpenTrophiesServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "OnOpenAchievements")]
    internal static class InventoryGuiOnOpenAchievementsServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(InventoryGui), "IsVisible")]
    internal static class InventoryGuiIsVisibleServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

}
