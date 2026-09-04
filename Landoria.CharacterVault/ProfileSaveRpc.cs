using System;
using HarmonyLib;
using Landoria.SharedLib;
using Splatform;
using TMPro;
using UnityEngine;

namespace Landoria.CharacterVault
{
    [HarmonyPatch(typeof(ZNet), "SaveWorldAndPlayerProfiles")]
    internal static class CharacterVaultManualSavePatch
    {
        private static void Prefix()
        {
            CharacterVaultPlugin.Transfers?.SaveManualClientProfile();
        }
    }

    [HarmonyPatch(typeof(Minimap), "Start")]
    internal static class CharacterVaultSaveStatusPatch
    {
        private static void Postfix(Minimap __instance)
        {
            CharacterVaultPlugin.SaveStatus?.Attach(__instance);
        }
    }

    [HarmonyPatch(typeof(ZNet), "Start")]
    internal static class PendingExitRequestPatch
    {
        private static void Postfix(ZNet __instance)
        {
            if (__instance.IsServer())
            {
                CharacterVaultPlugin.Settings?.InitializeServer();
            }
            CharacterVaultPlugin.Coordinator?.ProcessPendingExitRequest();
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class CharacterVaultConnectionPatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            CharacterVaultPlugin.Transfers?.Register(__instance, peer);
        }
    }

    [HarmonyPatch(typeof(ZNet), "SendPeerInfo")]
    [HarmonyAfter("Landoria.ModSentry")]
    internal static class CharacterVaultHelloPatch
    {
        private static void Prefix(ZRpc rpc)
        {
            if (ZNet.instance?.IsServer() == false)
            {
                CharacterVaultPlugin.Transfers?.SendHello(rpc);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    [HarmonyAfter("Landoria.ModSentry")]
    internal static class CharacterVaultAdmissionBarrierPatch
    {
        private static bool Prefix(ZRpc rpc, bool __runOriginal)
        {
            if (!__runOriginal || ZNet.instance?.IsServer() != true)
            {
                return true;
            }

            return CharacterVaultPlugin.Transfers?.ApproveGuest(rpc) == true;
        }
    }

    [HarmonyPatch(typeof(ZNet), "IsAllowed")]
    internal static class CharacterVaultPermittedListReasonPatch
    {
        private static void Postfix(
            string hostName,
            string playerName,
            SyncedList ___m_bannedList,
            SyncedList ___m_permittedList,
            Platform ___m_steamPlatform,
            bool __result)
        {
            CharacterVaultPlugin.Transfers?.RecordPermission(hostName, __result);
            if (__result || IsListed(___m_bannedList, hostName, ___m_steamPlatform) ||
                ___m_bannedList.Contains(playerName) || ___m_permittedList.Count() == 0 ||
                IsListed(___m_permittedList, hostName, ___m_steamPlatform))
            {
                return;
            }

            CharacterVaultRejection.RecordPermittedListRejection(hostName);
        }

        private static bool IsListed(SyncedList list, string value, Platform steamPlatform)
        {
            if (!PlatformUserID.TryParse(value, out PlatformUserID platformId))
            {
                platformId = new PlatformUserID(steamPlatform, value);
            }

            return list.Contains(platformId.ToString()) ||
                platformId.m_platform == steamPlatform && list.Contains(platformId.m_userID.ToString());
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class CharacterVaultPermittedListMessagePatch
    {
        private static void Postfix(ZRpc rpc)
        {
            if (ZNet.instance?.IsServer() == true)
            {
                CharacterVaultRejection.SendPermittedListRejection(rpc);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "Disconnect")]
    internal static class CharacterVaultDisconnectPatch
    {
        private static void Prefix(ZNetPeer peer)
        {
            bool server = ZNet.instance?.IsServer() == true;
            string state = CharacterVaultPlugin.Transfers?.DescribeDisconnect(peer, server) ??
                "state unavailable";
            CharacterVaultPlugin.Log?.LogInfo(
                $"Observed peer disconnection before CharacterVault cleanup: {state}.");
            if (!server)
            {
                CharacterVaultPlugin.Log?.LogInfo(
                    "Client ZNet.Disconnect call stack:\n" + Environment.StackTrace);
            }
            CharacterVaultPlugin.Transfers?.Remove(peer);
            if (peer?.m_rpc != null)
            {
                CharacterVaultRejection.Remove(peer.m_rpc);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class CharacterVaultClientNetworkDestroyPatch
    {
        private static void Prefix(ZNet __instance)
        {
            if (__instance.IsServer())
            {
                return;
            }

            ZNetPeer serverPeer = __instance.GetServerPeer();
            string state = CharacterVaultPlugin.Transfers?.DescribeDisconnect(
                serverPeer, false) ?? "state unavailable";
            CharacterVaultPlugin.Log?.LogInfo(
                $"Observed client network teardown before CharacterVault cleanup: {state}.");
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "ShowConnectError")]
    internal static class CharacterVaultShowRejectionPatch
    {
        private static void Postfix(TMP_Text ___m_connectionFailedError)
        {
            if (CharacterVaultRejection.TryGetMessage(out string message))
            {
                ___m_connectionFailedError.text = message;
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Start")]
    internal static class CharacterVaultShowRejectionAfterMenuLoadPatch
    {
        private static void Postfix(
            GameObject ___m_connectionFailedPanel,
            TMP_Text ___m_connectionFailedError)
        {
            if (CharacterVaultRejection.TryGetMessage(out string message))
            {
                ___m_connectionFailedError.text = message;
                ___m_connectionFailedPanel.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnConnectionFailedOk))]
    internal static class CharacterVaultAcknowledgeRejectionPatch
    {
        private static void Postfix()
        {
            CharacterVaultRejection.AcknowledgeClientMessage();
        }
    }

    [HarmonyPatch(typeof(ZNet), "InternalKick", new[] { typeof(ZNetPeer) })]
    internal static class CharacterVaultKickBarrierPatch
    {
        private static bool Prefix(ZNet __instance, ZNetPeer peer)
        {
            return CharacterVaultPlugin.ServerDisconnects?.AllowKick(__instance, peer) ?? true;
        }
    }

    [HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
    internal static class CharacterVaultProfileSavedPatch
    {
        private static void Postfix(PlayerProfile __instance, bool __result)
        {
            if (__result)
            {
                CharacterVaultPlugin.Transfers?.UploadSavedProfile(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Shutdown")]
    internal static class CharacterVaultDirectShutdownPatch
    {
        private static bool Prefix(Game __instance, bool saveWorld)
        {
            CharacterVaultPlugin.Log.LogInfo(
                $"Game.Shutdown invoked: saveWorld={saveWorld}, " +
                $"pendingCharacterSave={CharacterVaultPlugin.DisconnectCoordinator?.HasPendingSave == true}. " +
                "Call stack:\n" + Environment.StackTrace);
            return true;
        }
    }

    [HarmonyPatch(typeof(Game), "Logout")]
    internal static class CharacterVaultLogoutDiagnosticsPatch
    {
        private static bool Prefix(Game __instance, bool save, bool changeToStartScene)
        {
            CharacterVaultPlugin.Log.LogInfo(
                $"Game.Logout invoked: save={save}, changeToStartScene={changeToStartScene}, " +
                $"pendingCharacterSave={CharacterVaultPlugin.DisconnectCoordinator?.HasPendingSave == true}. " +
                "Call stack:\n" + Environment.StackTrace);
            CharacterVaultPlugin.Log.LogInfo(
                "Game.Logout is not intercepted; the vanilla logout continues immediately.");
            return true;
        }
    }

    [HarmonyPatch(typeof(Game), "ContinueLogout")]
    internal static class CharacterVaultContinueLogoutDiagnosticsPatch
    {
        private static void Prefix(bool save, bool shouldExit, bool changeToStartScene)
        {
            CharacterVaultPlugin.Log.LogInfo(
                $"Game.ContinueLogout invoked: save={save}, shouldExit={shouldExit}, " +
                $"changeToStartScene={changeToStartScene}, " +
                $"pendingCharacterSave={CharacterVaultPlugin.DisconnectCoordinator?.HasPendingSave == true}. " +
                "Call stack:\n" + Environment.StackTrace);
        }
    }

    [HarmonyPatch(typeof(Game), "OnDestroy")]
    internal static class CharacterVaultGameDestroyDiagnosticsPatch
    {
        private static void Prefix()
        {
            CharacterVaultPlugin.Log.LogInfo(
                $"Game.OnDestroy invoked: " +
                $"pendingCharacterSave={CharacterVaultPlugin.DisconnectCoordinator?.HasPendingSave == true}. " +
                "Call stack:\n" + Environment.StackTrace);
        }
    }

    [HarmonyPatch(typeof(Menu), "QuitGame")]
    internal static class CharacterVaultMenuQuitPatch
    {
        private static bool Prefix()
        {
            return CharacterVaultPlugin.DisconnectCoordinator?.AllowMenuQuit() ?? true;
        }
    }

    [HarmonyPatch(typeof(Menu), nameof(Menu.OnLogout))]
    internal static class CharacterVaultPrepareLogoutButtonPatch
    {
        private static bool Prefix(Menu __instance)
        {
            CharacterVaultPlugin.Log?.LogWarning(
                "The in-game Disconnect button was pressed. " +
                "Call stack:\n" + Environment.StackTrace);
            return CharacterVaultPlugin.DisconnectCoordinator?.AllowLogoutPrompt(__instance) ?? true;
        }
    }

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    internal static class CharacterVaultStartingItemsPatch
    {
        private static void Postfix(Player __instance)
        {
            CharacterVaultPlugin.DisconnectCoordinator?.RecordPlayerSpawned();
            CharacterVaultPlugin.Transfers?.RecordPlayerSpawned(__instance);
        }
    }

    [HarmonyPatch(typeof(Game), "SpawnPlayer")]
    internal static class CharacterVaultApplyProfilePatch
    {
        private static void Prefix(ref PlayerProfile ___m_playerProfile)
        {
            CharacterVaultPlugin.Transfers?.ApplyPendingProfile(ref ___m_playerProfile);
        }
    }

    [HarmonyPatch(typeof(ZNet), "Save")]
    internal static class CharacterVaultWorldSavePatch
    {
        private static void Prefix(ZNet __instance)
        {
            WorldSavePolicy.Handle(__instance.IsServer(),
                () => CharacterVaultPlugin.Transfers?.RecordOnlineActivityAtWorldSave(),
                () => CharacterVaultPlugin.Transfers?.RequestWorldCheckpoint());
        }
    }
}
