using System;
using HarmonyLib;
using Landoria.SharedLib;
using Splatform;
using TMPro;
using UnityEngine;

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

    [HarmonyPatch(typeof(ZNet), "IsAllowed")]
    [HarmonyBefore("Landoria.CharacterVault")]
    internal static class AllowGuestPastPermittedListPatch
    {
        private static void Postfix(string hostName, string playerName,
            SyncedList ___m_bannedList, Platform ___m_steamPlatform, ref bool __result)
        {
            if (!GuestAdmissions.IsGuest(hostName))
            {
                return;
            }

            bool banned = IsListed(___m_bannedList, hostName, ___m_steamPlatform) ||
                ___m_bannedList.Contains(playerName);
            __result = GuestPermissionPolicy.Resolve(__result, true, banned);
            ModSentryPlugin.Log.LogInfo(banned
                ? "Preserved the banned-list rejection for a guest."
                : "Allowed a guest past the server permitted list.");
        }

        private static bool IsListed(SyncedList list, string value, Platform platform)
        {
            if (!PlatformUserID.TryParse(value, out PlatformUserID platformId))
            {
                platformId = new PlatformUserID(platform, value);
            }

            return list.Contains(platformId.ToString()) ||
                platformId.m_platform == platform && list.Contains(platformId.m_userID.ToString());
        }
    }

    [HarmonyPatch(typeof(ZNet), "SendPeerInfo")]
    internal static class SendInventoryPatch
    {
        private static void Prefix(ZRpc rpc)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer())
            {
                ModSentryHandshake.SendInventory(rpc);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
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
            SetGuestMarker(rpc, GuestAdmissions.IsGuest(rpc));
            SetVerifiedMarker(rpc, HandshakeState.IsAccepted(rpc));
        }

        private static void SetGuestMarker(ZRpc rpc, bool marked)
        {
            if (marked)
            {
                ModSentryGuestMarker.Mark(rpc);
                return;
            }
            ModSentryGuestMarker.Unmark(rpc);
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
                HandshakeState.Remove(peer.m_rpc);
                VerifiedModpackMarker.Unmark(peer.m_rpc);
                PendingDisconnects.Remove(peer.m_rpc);
                GuestAdmissions.Remove(peer.m_rpc);
                VerifiedCharacterPositions.Remove(peer.m_rpc);
            }
            if (ZNet.instance?.IsServer() != true)
            {
                ClientVerificationState.Clear();
                ManagedCheatDetector.Shutdown();
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    internal static class RememberVerifiedCharacterPositionPatch
    {
        private static void Prefix(Player __instance)
        {
            CharacterPositionMemory.Save(__instance);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "ShowConnectError")]
    internal static class ShowRejectionPatch
    {
        private static void Postfix(TMP_Text ___m_connectionFailedError)
        {
            if (ClientMessage.TryGet(out string message))
            {
                ___m_connectionFailedError.text = message;
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Start")]
    internal static class ShowRejectionAfterMenuLoadPatch
    {
        private static void Postfix(
            GameObject ___m_connectionFailedPanel,
            TMP_Text ___m_connectionFailedError)
        {
            if (ClientMessage.TryGet(out string message))
            {
                ___m_connectionFailedError.text = message;
                ___m_connectionFailedPanel.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnConnectionFailedOk))]
    internal static class AcknowledgeRejectionPatch
    {
        private static void Postfix()
        {
            ClientMessage.Acknowledge();
        }
    }
}
