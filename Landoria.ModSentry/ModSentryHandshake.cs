using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.SharedLib;

namespace Landoria.ModSentry
{
    internal static class ModSentryHandshake
    {
        internal static void Register(ZNet network, ZNetPeer peer)
        {
            if (network.IsServer())
            {
                peer.m_rpc.Register<ZPackage>(ModSentryPlugin.InventoryRpc, ReceiveInventory);
                peer.m_rpc.Register(ModSentryPlugin.RejectionAckRpc, ReceiveRejectionAck);
                peer.m_rpc.Register<ZPackage>(ModSentryPlugin.CharacterPositionRpc,
                    VerifiedCharacterPositions.Receive);
                if (ModSentrySettings.KnownCheatProtectionEnabled)
                {
                    peer.m_rpc.Register<ZPackage>(
                        ModSentryPlugin.CheatDetectionRpc,
                        KnownCheatReport.Receive);
                }
            }
            else
            {
                ClientMessage.Clear();
                ClientVerificationState.Begin(peer.m_rpc);
                peer.m_rpc.Register<string>(ModSentryPlugin.RejectionRpc, ClientMessage.Receive);
                peer.m_rpc.Register(ModSentryPlugin.AcceptanceRpc,
                    ClientVerificationState.Accept);
                peer.m_rpc.Register(ModSentryPlugin.CheatDetectionEnableRpc,
                    ReceiveCheatDetectionEnable);
            }
        }

        internal static void SendInventory(ZRpc serverRpc)
        {
            serverRpc.Invoke(ModSentryPlugin.InventoryRpc, PluginInventory.Serialize());
        }

        internal static void ReceiveInventory(ZRpc rpc, ZPackage package)
        {
            try
            {
                IReadOnlyList<PluginDescriptor> inventory = PluginInventory.Deserialize(package);
                ValidationResult result = PolicyValidator.Validate(
                    ModSentryPlugin.EnsurePolicy(), inventory);
                Record(rpc, result);
            }
            catch (Exception exception)
            {
                ValidationResult result = ValidationResult.Reject(
                    "The installed mods could not be verified.",
                    $"Client inventory parsing failed: {exception}");
                Record(rpc, result);
            }
        }

        internal static bool Admit(ZRpc rpc)
        {
            if (HandshakeState.IsAccepted(rpc))
            {
                return true;
            }

            ValidationResult rejection = HandshakeState.RejectionFor(rpc);
            string failure = null;
            if (rejection == null && UnverifiedGuestControllerRegistry.IsReady &&
                GuestAdmissions.TryAdd(rpc, out failure))
            {
                ModSentryPlugin.Log.LogWarning(
                    "Admitting a client without a ModSentry inventory as a guest.");
                return true;
            }
            if (rejection == null && !string.IsNullOrEmpty(failure))
            {
                ModSentryPlugin.Log.LogError(
                    $"The server-only guest controller rejected admission: {failure}");
            }
            if (rejection == null)
            {
                LogUnavailableGuestAdmission();
            }
            rejection = rejection ?? ValidationResult.Reject(
                "Mod verification did not complete. Please try again.",
                "PeerInfo arrived before an accepted ModSentry inventory.");
            rpc.Invoke(ModSentryPlugin.RejectionRpc, rejection.PlayerMessage);
            ModSentryPlugin.Log.LogWarning(rejection.TechnicalMessage);
            PendingDisconnects.Schedule(rpc);
            return false;
        }

        private static void LogUnavailableGuestAdmission()
        {
            string reason = !UnverifiedGuestControllerRegistry.IsRegistered
                ? "the server-only guest controller is not registered"
                : !UnverifiedGuestControllerRegistry.IsReady
                    ? "the server-only guest controller is not ready"
                : "the server-only guest controller rejected admission";
            ModSentryPlugin.Log.LogWarning(
                $"Rejecting a client without a ModSentry inventory because {reason}.");
        }

        internal static void RequestDisconnect(ZRpc rpc)
        {
            ModSentryPlugin.Log.LogDebug(
                "Requesting rejected pre-spawn client disconnection.");
            rpc?.Invoke("Disconnect");
        }

        internal static void ForceDisconnect(ZRpc rpc)
        {
            ZNetPeer peer = ZNet.instance?.GetPeers()
                .FirstOrDefault(candidate => ReferenceEquals(candidate.m_rpc, rpc));
            if (peer != null)
            {
                ModSentryPlugin.Log.LogWarning(
                    "Rejected client did not disconnect; closing the server connection.");
                ZNet.instance.Disconnect(peer);
            }
        }

        internal static string Describe(ZNetPeer peer)
        {
            return string.IsNullOrWhiteSpace(peer?.m_playerName)
                ? "with an unavailable player name" : $"'{peer.m_playerName}'";
        }

        private static void ReceiveRejectionAck(ZRpc rpc)
        {
            PendingDisconnects.Acknowledge(rpc);
        }

        private static void ReceiveCheatDetectionEnable(ZRpc rpc)
        {
            ManagedCheatDetector.Enable(rpc);
        }

        private static void Record(ZRpc rpc, ValidationResult result)
        {
            if (result.Accepted)
            {
                HandshakeState.Accept(rpc);
                VerifiedModpackMarker.Mark(rpc);
                if (ModSentrySettings.KnownCheatProtectionEnabled)
                {
                    rpc.Invoke(ModSentryPlugin.CheatDetectionEnableRpc);
                }
                rpc.Invoke(ModSentryPlugin.AcceptanceRpc);
                ModSentryPlugin.Log.LogInfo(result.TechnicalMessage);
                return;
            }

            HandshakeState.Reject(rpc, result);
            ModSentryPlugin.Log.LogWarning(result.TechnicalMessage);
        }
    }
}
