using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Removes a saved password when it is rejected.
    [HarmonyPatch(typeof(FejdStartup), "ShowConnectError")]
    internal static class InvalidPasswordPatch
    {
        // Clears rejected password data before showing the error.
        private static void Prefix(ZNet.ConnectionStatus statusOverride)
        {
            ZNet.ConnectionStatus status = statusOverride == ZNet.ConnectionStatus.None
                ? ZNet.GetConnectionStatus() : statusOverride;
            RememberedPassword.HandleConnectionError(status);
        }
    }
}
