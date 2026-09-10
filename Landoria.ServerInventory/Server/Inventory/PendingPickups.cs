using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class PendingPickups
    {
        private sealed class Pending
        {
            internal ZNetPeer Peer;
            internal ZDOID Character;
            internal ItemDrop Drop;
            internal JObject Request;
            internal float Deadline;
        }

        private static readonly List<Pending> requests = new List<Pending>();

        internal static bool Defer(ZNetPeer peer, JObject request)
        {
            if ((string)request["kind"] != "pickup") return false;
            var id = new ZDOID(long.Parse((string)request["user"]), (uint)request["object"]);
            var drop = ZNetScene.instance.FindInstance(id)?.GetComponent<ItemDrop>();
            if (drop == null || !drop.CanPickup(false) || drop.CanPickup()) return false;
            if (requests.Count >= 4096 || requests.Count(value => value.Peer == peer) >= 64)
                throw new InvalidOperationException("Too many pending pickup requests.");
            requests.Add(new Pending { Peer = peer, Character = peer.m_characterID, Drop = drop,
                Request = request, Deadline = Time.realtimeSinceStartup + 2f });
            return true;
        }

        internal static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) { requests.Clear(); return; }
            for (int index = requests.Count - 1; index >= 0; index--)
            {
                var pending = requests[index];
                try
                {
                    if (!ZNet.instance.GetPeers().Contains(pending.Peer) || !pending.Peer.IsReady() ||
                        pending.Peer.m_characterID != pending.Character)
                    { requests.RemoveAt(index); continue; }
                    if (InventoryChangesServer.IsLoaded(pending.Peer.m_rpc) && pending.Drop != null &&
                        pending.Drop.CanPickup(false) && !pending.Drop.CanPickup() &&
                        Time.realtimeSinceStartup < pending.Deadline) continue;
                    requests.RemoveAt(index);
                    if (!InventoryChangesServer.IsLoaded(pending.Peer.m_rpc))
                        pending.Peer.m_rpc.Invoke(CharacterRpc.WorldActionResult, (string)pending.Request["id"],
                            false, "Inventory is unavailable.", new ZPackage());
                    else ServerWorldActions.Execute(pending.Peer, pending.Request);
                }
                catch (Exception error)
                {
                    requests.Remove(pending);
                    CharacterRpc.Log.LogError(error);
                }
            }
        }
    }
}
