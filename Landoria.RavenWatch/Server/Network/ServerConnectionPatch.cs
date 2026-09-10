using System;
using HarmonyLib;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Server.Network
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class ServerConnectionPatch
    {
        private static void Postfix(ZNetPeer peer)
        {
            if (!ZNet.instance.IsDedicated()) return;
            try { InventoryPollServer.Register(peer); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
