using System.Collections.Generic;
using HarmonyLib;

namespace Landoria.StructureProtection
{
    internal static class StructureProtectionSession
    {
        private const string IdentityRpc = "Landoria_StructureProtection_Identity";
        private const string SnapshotRpc = "Landoria_StructureProtection_Snapshot";
        private static readonly Dictionary<long, long> PeerPlayers = new Dictionary<long, long>();
        private static readonly HashSet<long> OnlinePlayers = new HashSet<long>();
        private static ZRoutedRpc registeredRpc;
        private static long identityServer;

        internal static void Update()
        {
            RegisterRpcs();
            SendLocalIdentity();
            if (ZNet.instance != null && ZNet.instance.IsServer() && RemoveDisconnectedPeers())
            {
                BroadcastSnapshot();
            }
        }

        internal static void Reset()
        {
            registeredRpc = null;
            identityServer = 0L;
            ClearState();
        }

        private static void ClearState()
        {
            PeerPlayers.Clear();
            OnlinePlayers.Clear();
            WardQuota.ResetClientState();
            StructureProtectionPlugin.Settings?.ResetClientState();
        }

        internal static HashSet<long> GetOnlinePlayers()
        {
            return new HashSet<long>(OnlinePlayers);
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
            ClearState();
            registeredRpc = rpc;
            identityServer = 0L;
        }

        private static void SendLocalIdentity()
        {
            ZNet network = ZNet.instance;
            Player player = Player.m_localPlayer;
            if (network == null || player == null || network.IsServer() || registeredRpc == null)
            {
                return;
            }
            ZNetPeer server = network.GetServerPeer();
            if (server == null || server.m_uid == identityServer)
            {
                return;
            }
            identityServer = server.m_uid;
            registeredRpc.InvokeRoutedRPC(server.m_uid, IdentityRpc, player.GetPlayerID());
        }

        private static void ReceiveIdentity(long sender, long playerId)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() ||
                ZNet.instance.GetPeer(sender) == null || playerId == 0L)
            {
                return;
            }
            PeerPlayers[sender] = playerId;
            OnlinePlayers.Add(playerId);
            BroadcastSnapshot();
            WardQuota.RegisterIdentity(sender, playerId);
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
                OnlinePlayers.Remove(PeerPlayers[peer]);
                WardQuota.RemoveIdentity(PeerPlayers[peer]);
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
            ZPackage package = new ZPackage();
            WriteMappings(package);
            registeredRpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, SnapshotRpc, package);
        }

        private static void WriteMappings(ZPackage package)
        {
            StructureProtectionPlugin.Settings.WriteClientState(package);
            package.Write(PeerPlayers.Count);
            foreach (KeyValuePair<long, long> mapping in PeerPlayers)
            {
                package.Write(mapping.Value);
            }
        }

        private static void ReceiveSnapshot(long sender, ZPackage package)
        {
            if (!IsTrustedServerCore(sender))
            {
                return;
            }
            StructureProtectionPlugin.Settings.ReadClientState(package);
            OnlinePlayers.Clear();
            int count = package.ReadInt();
            for (int index = 0; index < count; index++)
            {
                long player = package.ReadLong();
                OnlinePlayers.Add(player);
            }
        }

        private static bool IsTrustedServerCore(long sender)
        {
            ZNet network = ZNet.instance;
            if (network == null || network.IsServer())
            {
                return network != null && network.IsServer();
            }
            return network.GetServerPeer()?.m_uid == sender;
        }

        internal static bool IsTrustedServer(long sender)
        {
            return IsTrustedServerCore(sender);
        }

        internal static bool TryGetPeer(long playerId, out long peerId)
        {
            foreach (KeyValuePair<long, long> mapping in PeerPlayers)
            {
                if (mapping.Value == playerId)
                {
                    peerId = mapping.Key;
                    return true;
                }
            }
            peerId = 0L;
            return false;
        }

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        private static class PlayerSpawnPatch
        {
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    identityServer = 0L;
                    Update();
                }
            }
        }
    }
}
