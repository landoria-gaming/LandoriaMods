using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Changes the menu startup behavior.
    [HarmonyPatch(typeof(FejdStartup), "Start")]
    internal static class StartPatch
    {
        // Skips the startup introduction.
        private static void Prefix()
        {
            if (CinematicsManager.s_instance != null)
            {
                CinematicsManager.s_instance.m_introOnStartup = false;
            }
        }

        // Starts automatic loading after the menu is ready.
        private static void Postfix(FejdStartup __instance)
        {
            QuickLaunchSession.Log.LogDebug("QuickLaunch FejdStartup.Start postfix invoked.");
            QuickLaunchSession.StartLastSession(__instance);
        }
    }
}
