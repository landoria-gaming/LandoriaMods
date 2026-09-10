using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(Minimap), "Update")]
    internal static class MinimapUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Minimap), "IsOpen")]
    internal static class MinimapIsOpenServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

}
