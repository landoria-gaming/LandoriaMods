using System;
using HarmonyLib;
using Landoria.SharedLib;

namespace Landoria.ModSentry
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class RegisterHandshakePatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            ModSentryHandshake.Register(__instance, peer);
        }
    }

    [HarmonyPatch(typeof(ZNet), "SendPeerInfo")]
    [HarmonyBefore("Landoria.CharacterVault")]
    internal static class SendInventoryPatch
    {
        private static bool Prefix(ZNet __instance, ZRpc rpc, string __1)
        {
            return __instance.IsServer() || NonceHandshake.AllowPeerInfo(__instance, rpc, __1);
        }

    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    [HarmonyBefore("Landoria.CharacterVault")]
    internal static class ValidatePeerPatch
    {
        private static bool Prefix(ZRpc rpc)
        {
            return ZNet.instance == null || !ZNet.instance.IsServer() ||
                   ModSentryHandshake.Admit(rpc);
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_ServerSyncedPlayerData")]
    internal static class RestoreServerAdmissionMarkersPatch
    {
        private static void Postfix(ZRpc rpc)
        {
            SetVerifiedMarker(rpc, HandshakeState.IsAccepted(rpc));
        }

        private static void SetVerifiedMarker(ZRpc rpc, bool marked)
        {
            if (marked)
            {
                VerifiedModpackMarker.Mark(rpc);
                return;
            }
            VerifiedModpackMarker.Unmark(rpc);
        }
    }

    [HarmonyPatch(typeof(ZNet), "Disconnect")]
    internal static class ClearHandshakePatch
    {
        private static void Prefix(ZNetPeer peer)
        {
            if (peer?.m_rpc != null)
            {
                NonceHandshake.Remove(peer.m_rpc);
                HandshakeState.Remove(peer.m_rpc);
                VerifiedModpackMarker.Unmark(peer.m_rpc);
                PendingDisconnects.Remove(peer.m_rpc);
            }
            if (ZNet.instance?.IsServer() != true)
            {
                ManagedCheatDetector.Shutdown();
            }
        }
    }


}
