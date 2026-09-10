using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(BuildUi), "Update")]
    internal static class BuildUiUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(BuildUi), "AddRecentPiece")]
    internal static class BuildUiAddRecentPieceServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(BuildUi), "OpenBuildMenu")]
    internal static class BuildUiOpenBuildMenuServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(BuildUi), "Close")]
    internal static class BuildUiCloseServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

}
