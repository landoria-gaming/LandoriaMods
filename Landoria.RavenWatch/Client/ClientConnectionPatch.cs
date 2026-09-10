using System;
using HarmonyLib;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class ClientConnectionPatch
    {
        private static void Postfix(ZNetPeer peer)
        {
            if (ZNet.instance.IsServer()) return;
            try { InventoryPollClient.Register(peer); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
