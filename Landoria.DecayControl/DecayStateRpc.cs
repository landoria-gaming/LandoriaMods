using System.Collections.Generic;
using Landoria.SharedLib;

namespace Landoria.DecayControl
{
    internal static class DecayStateRpc
    {
        private const string IdentityRpc = "Landoria_DecayControl_Identity";
        private const string SnapshotRpc = "Landoria_DecayControl_Snapshot";
        private static readonly Dictionary<long, long> PeerPlayers =
            new Dictionary<long, long>();
        private static ZRoutedRpc registeredRpc;
        private static long identityServer;

        internal static void Update()
        {
            RegisterRpcs();
            SendLocalIdentity();
            if (UsesPlayerActivity() && ZNet.instance != null &&
                ZNet.instance.IsServer() &&
                RemoveDisconnectedPeers())
            {
                BroadcastSnapshot();
            }
        }

        internal static void RequestOnSpawn()
        {
            identityServer = 0L;
            Update();
        }

        internal static void ResetSession()
        {
            registeredRpc = null;
            identityServer = 0L;
            PeerPlayers.Clear();
            DecayControlPlugin.Settings?.ResetState();
            DecayProtection.Reset();
        }

        internal static HashSet<long> GetOnlinePlayers()
        {
            return new HashSet<long>(PeerPlayers.Values);
        }

        private static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || ReferenceEquals(rpc, registeredRpc))
            {
                return;
            }
            rpc.Register<long>(IdentityRpc, ReceiveIdentity);
            rpc.Register<ZPackage>(SnapshotRpc, ReceiveSnapshot);
            PeerPlayers.Clear();
            DecayProtection.Reset();
            registeredRpc = rpc;
            identityServer = 0L;
        }

        private static void SendLocalIdentity()
        {
            ZNet network = ZNet.instance;
            Player player = Player.m_localPlayer;
            if (network == null || player == null || network.IsServer() ||
                registeredRpc == null)
            {
                return;
            }
            ZNetPeer server = network.GetServerPeer();
            if (server == null || server.m_uid == identityServer)
            {
                return;
            }
            identityServer = server.m_uid;
            registeredRpc.InvokeRoutedRPC(
                server.m_uid, IdentityRpc, player.GetPlayerID());
        }

        private static void ReceiveIdentity(long sender, long playerId)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() ||
                ZNet.instance.GetPeer(sender) == null || playerId == 0L)
            {
                return;
            }
            if (!UsesPlayerActivity())
            {
                SendSnapshot(sender);
                return;
            }
            PeerPlayers[sender] = playerId;
            BroadcastSnapshot();
        }

        private static bool RemoveDisconnectedPeers()
        {
            bool changed = false;
            foreach (long peer in new List<long>(PeerPlayers.Keys))
            {
                if (ZNet.instance.GetPeer(peer)?.IsReady() == true)
                {
                    continue;
                }
                PeerPlayers.Remove(peer);
                changed = true;
            }
            return changed;
        }

        private static void BroadcastSnapshot()
        {
            if (registeredRpc == null)
            {
                return;
            }
            SendSnapshot(ZRoutedRpc.Everybody);
        }

        private static void SendSnapshot(long target)
        {
            ZPackage package = new ZPackage();
            DecayControlPlugin.Settings.WriteState(package);
            bool includesActiveCreators = UsesPlayerActivity();
            package.Write(includesActiveCreators);
            if (includesActiveCreators)
            {
                DecayProtection.WriteState(package, PeerPlayers.Values);
            }
            registeredRpc.InvokeRoutedRPC(target, SnapshotRpc, package);
        }

        private static void ReceiveSnapshot(long sender, ZPackage package)
        {
            if (!IsTrustedServer(sender))
            {
                return;
            }
            DecayControlPlugin.Settings.ReadState(package);
            if (package.ReadBool())
            {
                DecayProtection.ReadState(package);
            }
            else
            {
                DecayProtection.ClearActivityState();
            }
            DecayControlPlugin.Log?.LogInfo(
                $"Received server decay settings: fuelConsumption=" +
                $"{DecayControlPlugin.Settings.FuelConsumption}, " +
                $"environmentalBuildingWear=" +
                $"{DecayControlPlugin.Settings.EnvironmentalBuildingWear}, " +
                $"activeCreators={DecayProtection.ActiveCreatorCount}.");
        }

        private static bool UsesPlayerActivity()
        {
            return DecayControlPlugin.Settings?.UsesPlayerActivity == true;
        }

        private static bool IsTrustedServer(long sender)
        {
            ZNet network = ZNet.instance;
            if (network == null || network.IsServer())
            {
                return network != null && network.IsServer();
            }
            return network.GetServerPeer()?.m_uid == sender;
        }
    }
}
