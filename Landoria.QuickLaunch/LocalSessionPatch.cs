using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Records local world starts.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnWorldStart))]
    internal static class LocalSessionPatch
    {
        // Saves the local session before the world starts.
        private static void Prefix() => QuickLaunchSession.RecordLocalSession();
    }
}
