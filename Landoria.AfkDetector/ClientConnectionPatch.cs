using HarmonyLib;

namespace Landoria.AfkDetector
{
    // Registers the AFK disconnect message on clients.
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class ClientConnectionPatch
    {
        // Adds the message handler after a client connects.
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (!__instance.IsServer())
            {
                peer.m_rpc.Register<string>(ClientDisconnectReason.RpcName,
                    ClientDisconnectReason.Receive);
            }
        }
    }

}
