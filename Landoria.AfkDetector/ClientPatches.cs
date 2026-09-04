using HarmonyLib;

namespace Landoria.AfkDetector
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class ClientConnectionPatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (!__instance.IsServer())
            {
                peer.m_rpc.Register<string>(AfkDetectorPlugin.DisconnectReasonRpc,
                    ClientDisconnectReason.Receive);
            }
        }
    }

}
