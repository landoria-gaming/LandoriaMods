using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class AutomaticPickup
    {
        private static readonly ConditionalWeakTable<ZRpc, Dictionary<ZDOID, string>> reports =
            new ConditionalWeakTable<ZRpc, Dictionary<ZDOID, string>>();

        internal static bool Check(WorldActionTransaction action, ItemDrop drop)
        {
            float distance = Vector3.Distance(action.Player.transform.position + Vector3.up, drop.transform.position);
            float range = action.Player.m_autoPickupRange;
            float weight = action.Inventory.GetTotalWeight(), itemWeight = drop.m_itemData.GetWeight();
            float maximum = action.Player.GetMaxCarryWeight();
            string reason = !drop.m_autoPickup ? "disabled" : distance > range ? "distance" :
                weight + itemWeight > maximum ? "weight" :
                !action.Inventory.CanAddItem(drop.m_itemData, drop.m_itemData.m_stack) ? "inventory_full" : null;
            var id = drop.GetComponent<ZNetView>().GetZDO().m_uid;
            var reported = reports.GetOrCreateValue(action.Peer.m_rpc);
            if (reason == null) { reported.Remove(id); return true; }
            if (!reported.TryGetValue(id, out var previous) || previous != reason)
            {
                if (reported.Count >= 256) reported.Clear();
                reported[id] = reason;
                CharacterRpc.Log.LogInfo($"Automatic pickup skipped: object={id}, prefab={drop.gameObject.name}, " +
                    $"reason={reason}, autoPickup={drop.m_autoPickup}, distance={distance:F3}, range={range:F3}, " +
                    $"weight={weight:F2}, itemWeight={itemWeight:F2}, capacity={maximum:F2}, " +
                    $"playerPosition={action.Player.transform.position:F2}, objectPosition={drop.transform.position:F2}.");
            }
            return false;
        }
    }
}
