using System.Collections;
using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(FejdStartup), "PlayIntroCinematic")]
    internal static class StartupCinematicPatch
    {
        private static bool Prefix(ref IEnumerator __result)
        {
            // Startup has no ZNet yet; use the native dedicated startup graphics check.
            if (!ServerUi.Disabled) return true;
            __result = SkipCinematic();
            return false;
        }

        private static IEnumerator SkipCinematic()
        {
            yield break;
        }
    }
}
