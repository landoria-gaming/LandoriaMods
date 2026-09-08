using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal static class ServerGroundItemPickupCollector
    {
        private const float MaximumPickupDistance = 4f;
        private static readonly int DestroyZdoRpc = "DestroyZDO".GetStableHashCode();

        internal static void Observe(ZRpc rpc, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || package == null) return;
            try
            {
                ZNetPeer sender = ZNet.instance.GetPeers().FirstOrDefault(peer => peer.m_rpc == rpc);
                if (sender == null || sender.m_characterID.IsNone()) return;
                var data = new ZRoutedRpc.RoutedRPCData();
                data.Deserialize(new ZPackage(package.GetArray()));
                if (data.m_senderPeerID != sender.m_uid || data.m_methodHash != DestroyZdoRpc)
                    return;
                RecordRemovedItems(sender, new ZPackage(data.m_parameters.GetArray()).ReadPackage());
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }

        private static void RecordRemovedItems(ZNetPeer sender, ZPackage removed)
        {
            ZDO player = ZDOMan.instance?.GetZDO(sender.m_characterID);
            if (player == null) return;
            Vector3 playerPosition = player.GetPosition();
            int count = removed.ReadInt();
            for (int index = 0; index < count; index++)
                RecordRemovedItem(sender, removed.ReadZDOID(), playerPosition);
        }

        private static void RecordRemovedItem(ZNetPeer sender, ZDOID id,
            Vector3 playerPosition)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            GameObject prefab = zdo == null || ZNetScene.instance == null
                ? null : ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            ItemDrop drop = prefab ? prefab.GetComponent<ItemDrop>() : null;
            if (!drop || zdo.GetOwner() != sender.m_uid) return;
            Vector3 itemPosition = zdo.GetPosition();
            float distance = Vector3.Distance(itemPosition, playerPosition);
            if (distance > MaximumPickupDistance) return;
            ServerEventPublisher.Publish(new GroundItemRemovedNearPlayerServer
            {
                item = InventoryItemSnapshot.Capture(drop.m_itemData, zdo, prefab.name),
                itemNetworkId = id.ToString(),
                playerName = sender.m_playerName, playerSessionId = sender.m_uid.ToString(),
                itemPosition = itemPosition, playerPosition = playerPosition,
                distanceFromPlayer = distance
            });
        }
    }

    [HarmonyPatch(typeof(ZRoutedRpc), "RPC_RoutedRPC")]
    internal static class ServerGroundItemPickupRoutedRpcPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ZRpc rpc, ZPackage pkg)
        {
            ServerGroundItemPickupCollector.Observe(rpc, pkg);
        }
    }
}
