using System;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    internal static class InventoryPollClient
    {
        internal static void Register(ZNetPeer peer)
            => peer.m_rpc.Register<string>(InventoryProtocol.Request, Reply);

        private static void Reply(ZRpc rpc, string requestId)
        {
            try
            {
                var player = Player.m_localPlayer;
                if (player == null || ZNet.instance?.GetServerPeer()?.m_rpc != rpc
                    || !Guid.TryParse(requestId, out _)) return;
                InventoryEventSender.Prepare(rpc, player.GetPlayerID());
                var package = new ZPackage();
                package.Write(requestId);
                package.Write(InventoryEventSender.Stream);
                package.Write(InventoryEventSender.Sequence);
                package.Write(InventorySnapshotCapture.Capture(player).GetArray());
                rpc.Invoke(InventoryProtocol.Response, player.GetPlayerID(), package);
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
