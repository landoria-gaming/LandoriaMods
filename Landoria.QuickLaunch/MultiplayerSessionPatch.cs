using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Records multiplayer server joins.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.JoinServer))]
    internal static class MultiplayerSessionPatch
    {
        // Saves the server details before joining.
        private static void Prefix(FejdStartup __instance) =>
            QuickLaunchSession.RecordMultiplayerSession(__instance);
    }
}
