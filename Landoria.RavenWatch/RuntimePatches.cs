using System;
using HarmonyLib;
using Landoria.RavenWatch.Client;
using Landoria.RavenWatch.Server;
using Landoria.RavenWatch.Server.Journal;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch
{
    [HarmonyPatch(typeof(ZNet), "Awake")]
    internal static class RuntimePatches
    {
        internal static Harmony Harmony;
        private static void Prefix(ZNet __instance)
        {
            try
            {
                if (__instance.IsDedicated()) ServerRuntime.Install(Harmony);
                else ClientRuntime.Install(Harmony);
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        private static void Postfix(ZNet __instance)
        {
            if (!__instance.IsDedicated()) return;
            try { RpcCapture.Start(); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
