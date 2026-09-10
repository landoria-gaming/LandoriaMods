using System;
using System.Linq;
using HarmonyLib;
using Landoria.SharedLib;
using Landoria.ServerInventory.Server;
using Landoria.ServerInventory.Client;

namespace Landoria.ServerInventory.Network
{
    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class CharacterRpc
    {
        internal const string ContainerSnapshot = "Landoria.ServerInventory.ContainerSnapshot";
        internal const string WorldAction = "Landoria.ServerInventory.WorldAction";
        internal const string WorldActionResult = "Landoria.ServerInventory.WorldActionResult";
        internal const string ReservePlayer = "Landoria.ServerInventory.ReservePlayer";
        internal const string ReservedPlayer = "Landoria.ServerInventory.ReservedPlayer";
        internal const string Spawn = "Landoria.ServerInventory.Spawn";
        internal const string SpawnResult = "Landoria.ServerInventory.SpawnResult";
        internal const string Discoveries = "Landoria.ServerInventory.Discoveries";
        internal const string Attack = "Landoria.ServerInventory.Attack";
        internal const string CombatResult = "Landoria.ServerInventory.CombatResult";
        internal const string CombatInventory = "Landoria.ServerInventory.CombatInventory";
        internal const string Request = "Landoria.ServerInventory.Load";
        internal const string Response = "Landoria.ServerInventory.Profile";
        internal static ModLog Log;

        private static void Prefix(ZNet __instance, ZNetPeer peer)
        {
            if (__instance.IsDedicated())
            {
                peer.m_rpc.Register<string, string>(Request, ReceiveRequest);
                InventoryChangesServer.Register(peer);
                peer.m_rpc.Register<string>(Discoveries, PlayerDiscoveriesServer.Receive);
                ServerAttacks.Register(peer);
                peer.m_rpc.Register<string>(Spawn, ServerSpawn.Receive);
                peer.m_rpc.Register<string>(WorldAction, ServerWorldActions.Receive);
                peer.m_rpc.Register<UnityEngine.Vector3>(ReservePlayer, ServerPlayerCreation.Receive);
            }
            else if (!__instance.IsServer())
            {
                peer.m_rpc.Register<string, ZPackage>(Response, CharacterLoad.Receive);
                peer.m_rpc.Register<ZPackage>(CombatResult, Client.CombatResult.Receive);
                peer.m_rpc.Register<ZPackage>(CombatInventory, Client.CombatResult.ReceiveInventory);
                peer.m_rpc.Register<string>(SpawnResult, SpawnRequest.Receive);
                peer.m_rpc.Register<string, bool, string, ZPackage>(WorldActionResult, WorldActionRequest.Receive);
                peer.m_rpc.Register<ZPackage>(ReservedPlayer, PlayerCreation.Receive);
                peer.m_rpc.Register<ZDOID, bool, ZPackage>(ContainerSnapshot, InventoryActionRequest.ReceiveContainer);
            }
        }

        private static void ReceiveRequest(ZRpc rpc, string requestId, string appearance)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(p => p.m_rpc == rpc);
            if (peer == null || !peer.IsReady() || !peer.m_characterID.IsNone()) return;
            if (!InventoryCommitLog.Healthy) { ZNet.instance.Disconnect(peer); return; }
            if (!Guid.TryParse(requestId, out _)) return;
            try
            {
                var response = new ZPackage();
                response.Write(CharacterStore.LoadOrCreate(peer, appearance));
                InventoryChangesServer.MarkLoaded(rpc);
                rpc.Invoke(Response, requestId, response);
            }
            catch (Exception error)
            {
                Log.LogError(error);
                ZNet.instance.Disconnect(peer);
            }
        }
    }
}
