using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal static class ServerPlayerDebugFlyCollector
    {
        private static readonly HashSet<ZDOID> active = new HashSet<ZDOID>();
        private static ZRpc currentRpc;

        internal static void Open()
        {
            active.Clear();
            currentRpc = null;
        }

        internal static void Begin(ZRpc rpc)
        {
            currentRpc = ZNet.instance != null && ZNet.instance.IsServer() ? rpc : null;
        }

        internal static void Observe(ZDO zdo)
        {
            if (currentRpc == null || zdo == null || ZNet.instance == null) return;
            try
            {
                ZNetPeer sender = ZNet.instance.GetPeers()
                    .FirstOrDefault(peer => peer.m_rpc == currentRpc);
                if (sender == null || sender.m_characterID != zdo.m_uid) return;
                if (!zdo.GetBool(ZDOVars.s_debugFly))
                {
                    active.Remove(zdo.m_uid);
                    return;
                }
                if (active.Add(zdo.m_uid))
                    ServerEventPublisher.Publish(PlayerDebugFlyServer.Capture(zdo, sender));
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }

        internal static void End() => currentRpc = null;

        internal static void Close()
        {
            currentRpc = null;
            active.Clear();
        }
    }

    [HarmonyPatch(typeof(ZDOMan), "RPC_ZDOData")]
    internal static class ServerPlayerDebugFlyPacketPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ZRpc rpc) => ServerPlayerDebugFlyCollector.Begin(rpc);

        [HarmonyFinalizer]
        private static void Finalizer() => ServerPlayerDebugFlyCollector.End();
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
    internal static class ServerPlayerDebugFlyZdoPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZDO __instance) => ServerPlayerDebugFlyCollector.Observe(__instance);
    }
}
