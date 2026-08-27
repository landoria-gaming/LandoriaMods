using System;
using Landoria.SharedLib;

namespace Landoria.HammerFreedom
{
    internal static class HammerFreedomAuthorization
    {
        private const string RequestRpc = "Landoria_HammerFreedom_Request";
        private const string ResponseRpc = "Landoria_HammerFreedom_Response";
        private static ZRoutedRpc _registeredRpc;
        private static ZNetPeer _serverPeer;
        private static bool _requestSent;
        private static HammerFreedomCapabilities _authorizedCapabilities;

        internal static bool IsAuthorized(HammerFreedomCapabilities capability)
        {
            return (_authorizedCapabilities & capability) == capability;
        }

        internal static void Update()
        {
            RegisterRpcs();
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                ResetConnection();
                return;
            }

            if (ZNet.instance.IsServer())
            {
                UpdateServerAuthorization();
            }
            else
            {
                UpdateClientAuthorization();
            }
        }

        internal static void ResetSession()
        {
            ResetConnection();
        }

        internal static void RequestOnSpawn()
        {
            RegisterRpcs();
            if (ZNet.instance == null || ZNet.instance.IsServer() ||
                ZRoutedRpc.instance == null || _requestSent)
            {
                return;
            }

            _serverPeer = ZNet.instance.GetServerPeer();
            if (_serverPeer == null)
            {
                return;
            }

            _requestSent = true;
            ZRoutedRpc.instance.InvokeRoutedRPC(_serverPeer.m_uid, RequestRpc);
        }

        private static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || ReferenceEquals(rpc, _registeredRpc))
            {
                return;
            }

            rpc.Register(RequestRpc, ReceiveRequest);
            rpc.Register<int>(ResponseRpc, ReceiveResponse);
            _registeredRpc = rpc;
        }

        private static void UpdateServerAuthorization()
        {
            if (!ServerRole.IsDedicatedServer)
            {
                SetAuthorized(HammerFreedomCapabilities.None);
                return;
            }
            HammerFreedomPlugin.InitializeDedicatedServerSettings();
            HammerFreedomCapabilities capabilities = ResolveServerCapabilities();
            SetAuthorized(capabilities);
        }

        private static void UpdateClientAuthorization()
        {
            if (!IsAuthorized(HammerFreedomCapabilities.Flight))
            {
                FlyController.SetEnabled(false);
            }

            ZNetPeer currentServer = ZNet.instance.GetServerPeer();
            if (!ReferenceEquals(currentServer, _serverPeer))
            {
                ResetConnection();
                _serverPeer = currentServer;
            }

        }

        private static void ReceiveRequest(long sender)
        {
            if (!ServerRole.IsDedicatedServer ||
                ZNet.instance.GetPeer(sender) == null || ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(
                sender, ResponseRpc, (int)ResolveServerCapabilities());
        }

        private static void ReceiveResponse(long sender, int capabilities)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() ||
                _serverPeer == null || _serverPeer.m_uid != sender)
            {
                return;
            }

            SetAuthorized((HammerFreedomCapabilities)capabilities);
        }

        private static HammerFreedomCapabilities ResolveServerCapabilities()
        {
            bool hammerWorld = ZoneSystem.instance != null &&
                ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost) &&
                ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs);
            HammerFreedomSettings settings = HammerFreedomPlugin.Settings;
            return HammerFreedomCapabilityPolicy.Resolve(
                hammerWorld, settings != null && settings.Flight,
                settings != null && settings.FallDamageImmunity,
                settings != null && settings.UnlimitedStamina,
                settings != null && settings.NoDurabilityLoss);
        }

        private static void ResetConnection()
        {
            _serverPeer = null;
            _requestSent = false;
            FlyController.SetEnabled(false);
            SetAuthorized(HammerFreedomCapabilities.None);
        }

        private static void SetAuthorized(HammerFreedomCapabilities capabilities)
        {
            if (_authorizedCapabilities == capabilities)
            {
                return;
            }

            _authorizedCapabilities = capabilities;
            FlyController.OnAuthorizationChanged(IsAuthorized(HammerFreedomCapabilities.Flight));
            HammerFreedomPlugin.ModLogger?.LogInfo(
                $"HammerFreedom authorization is now {capabilities}.");
        }
    }
}
