using HarmonyLib;

namespace Landoria.Socialize
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class GroupNewConnectionPatch
    {
        private static void Prefix(ZNetPeer peer)
        {
            if (peer != null)
            {
                GroupService.BeginPeerSession(peer.m_uid);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    internal static class GroupDisconnectPatch
    {
        private static void Prefix(ZNetPeer peer)
        {
            if (peer != null)
            {
                GroupService.DisconnectPeer(peer.m_uid);
            }
        }
    }
}
