using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Captures a password used to join a server.
    [HarmonyPatch(typeof(ZNet), "SendPeerInfo")]
    internal static class CapturePasswordPatch
    {
        // Captures the password before peer information is sent.
        private static void Prefix(ZNet __instance, string password) =>
            RememberedPassword.Capture(__instance, password);
    }
}
