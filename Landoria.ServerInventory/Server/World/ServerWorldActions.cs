using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerWorldActions
    {
        private sealed class Session
        {
            internal readonly HashSet<Guid> Seen = new HashSet<Guid>();
            internal readonly HashSet<string> CompletedPickups = new HashSet<string>();
        }
        private static readonly ConditionalWeakTable<ZRpc, Session> sessions = new ConditionalWeakTable<ZRpc, Session>();

        internal static void Receive(ZRpc rpc, string json)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc && value.IsReady());
            if (peer == null || !InventoryChangesServer.IsLoaded(rpc)) return;
            string requestId = "";
            try
            {
                if (json == null || json.Length > 8192) throw new InvalidDataException("Invalid world action request.");
                var request = JObject.Parse(json);
                requestId = (string)request["id"];
                if (!Guid.TryParse(requestId, out var id)) throw new InvalidDataException("Invalid action ID.");
                var session = sessions.GetOrCreateValue(rpc);
                if (session.Seen.Count > 100000) throw new InvalidDataException("Too many world actions.");
                if (!session.Seen.Add(id)) return;
                if (!PendingPickups.Defer(peer, request)) Execute(peer, request);
            }
            catch (Exception error)
            {
                CharacterRpc.Log.LogError(error);
                rpc.Invoke(CharacterRpc.WorldActionResult, requestId, false, error.Message, new ZPackage());
            }
        }

        internal static void Execute(ZNetPeer peer, JObject request)
        {
            string requestId = (string)request["id"];
            bool committed = false;
            try
            {
                var instance = ZNetScene.instance.FindInstance(peer.m_characterID);
                var player = instance == null ? null : instance.GetComponent<Player>();
                if (player == null) throw new InvalidOperationException("Character unavailable.");
                var session = sessions.GetOrCreateValue(peer.m_rpc);
                string pickup = PickupKey(peer, request);
                using (var action = new WorldActionTransaction(peer, player))
                using (var scope = new CombatScope(player))
                using (var inventory = new NativeCraftInventory(player, action.Inventory))
                {
                    if (pickup != null && session.CompletedPickups.Contains(pickup)) action.ReadOnly = true;
                    else if (!Apply(action, request))
                    {
                        peer.m_rpc.Invoke(CharacterRpc.WorldActionResult, requestId, false, "", new ZPackage());
                        return;
                    }
                    var result = action.Commit();
                    committed = true;
                    if (pickup != null) session.CompletedPickups.Add(pickup);
                    peer.m_rpc.Invoke(CharacterRpc.WorldActionResult, requestId, true, "", result);
                }
            }
            catch (Exception error)
            {
                CharacterRpc.Log.LogError(error);
                if (!committed) peer.m_rpc.Invoke(CharacterRpc.WorldActionResult, requestId, false, error.Message, new ZPackage());
            }
        }

        private static string PickupKey(ZNetPeer peer, JObject request)
        {
            if ((string)request["kind"] != "pickup") return null;
            var id = new ZDOID(long.Parse((string)request["user"]), (uint)request["object"]);
            return peer.m_characterID.ToString() + "/" + id;
        }

        private static bool Apply(WorldActionTransaction action, JObject request)
        {
            string kind = (string)request["kind"];
            if (kind == "tombstone") { ServerTombstone.Create(action); return true; }
            if ((float)action.Document["profile"]["playerData"]["health"] <= 0f || action.Player.IsDead())
                throw new InvalidOperationException("Character is dead.");
            if (kind == "drop") Drop(action, request);
            else if (kind == "build") ServerBuilding.Create(action, request);
            else if (kind == "interact") NativeWorldInteraction.Apply(action, request);
            else if (kind == "trade") ServerTrading.Trade(action, request);
            else if (kind == "craft") ServerCrafting.Craft(action, request);
            else if (kind == "pickup") return WorldInventoryActions.Pickup(action, request);
            else if (kind == "container") WorldInventoryActions.Container(action, request);
            else throw new InvalidDataException("Unsupported world action.");
            return true;
        }

        private static void Drop(WorldActionTransaction action, JObject request)
        {
            int amount = (int)request["amount"], hash = (int)request["prefab"], quality = (int)request["quality"];
            int variant = (int)request["variant"], x = (int)request["x"], y = (int)request["y"];
            var matches = action.Inventory.GetAllItems().Where(item => item.m_dropPrefab != null &&
                item.m_dropPrefab.name.GetStableHashCode() == hash && item.m_quality == quality && item.m_variant == variant).ToArray();
            var item = matches.FirstOrDefault(value => value.m_gridPos.x == x && value.m_gridPos.y == y);
            if (item == null && matches.Length == 1) item = matches[0];
            if (item == null || item.m_shared.m_questItem || amount < 1 || amount > item.m_stack)
                throw new InvalidOperationException("Requested drop is absent, ambiguous or cannot be dropped.");
            var transform = action.Player.transform;
            var droppedItem = item.Clone();
            droppedItem.m_equipped = false;
            var drop = ItemDrop.DropItem(droppedItem, amount, transform.position + transform.forward + Vector3.up, transform.rotation);
            if (drop == null) throw new InvalidOperationException("Could not create the dropped item.");
            drop.OnPlayerDrop();
            var body = drop.GetComponent<Rigidbody>();
            if (body != null) body.linearVelocity = (transform.forward + Vector3.up) * (item.GetWeight() >= 300f ? 0.5f : 5f);
            action.Inventory.RemoveItem(item, amount);
        }
    }
}
