using System;
using HarmonyLib;

namespace Landoria.RavenWatch
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class EventConnectionPatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (__instance.IsServer()) return;
            try { EventTransport.Register(peer); }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }

    [HarmonyPatch(typeof(ZNet), "Disconnect")]
    internal static class EventDisconnectPatch
    {
        private static void Prefix(ZNetPeer peer) => EventTransport.Disconnect(peer);
    }
}
