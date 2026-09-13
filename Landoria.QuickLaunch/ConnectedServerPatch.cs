using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Records successful server connections.
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class ConnectedServerPatch
    {
        // Saves the server after peer information is received.
        private static void Postfix(ZNet __instance) =>
            QuickLaunchSession.RememberConnectedServer(__instance);
    }
}
