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
        private sealed class Session { internal readonly HashSet<Guid> Seen = new HashSet<Guid>(); }
        private static readonly ConditionalWeakTable<ZRpc, Session> sessions = new ConditionalWeakTable<ZRpc, Session>();

        internal static void Receive(ZRpc rpc, string json)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc && value.IsReady());
            if (peer == null || !InventoryChangesServer.IsLoaded(rpc)) return;
            string requestId = "";
            bool committed = false;
            try
            {
                if (json == null || json.Length > 8192) throw new InvalidDataException("Invalid world action request.");
                var request = JObject.Parse(json);
                requestId = (string)request["id"];
                if (!Guid.TryParse(requestId, out var id)) throw new InvalidDataException("Invalid action ID.");
                var session = sessions.GetOrCreateValue(rpc);
                if (session.Seen.Count > 100000) throw new InvalidDataException("Too many world actions.");
                if (!session.Seen.Add(id)) return;
                var instance = ZNetScene.instance.FindInstance(peer.m_characterID);
                var player = instance == null ? null : instance.GetComponent<Player>();
                if (player == null) throw new InvalidOperationException("Character unavailable.");
                using (var action = new WorldActionTransaction(peer, player))
                using (var scope = new CombatScope(player))
                using (var inventory = new NativeCraftInventory(player, action.Inventory))
                {
                    Apply(action, request);
                    var result = action.Commit();
                    committed = true;
                    rpc.Invoke(CharacterRpc.WorldActionResult, requestId, true, "", result);
                }
            }
            catch (Exception error)
            {
                CharacterRpc.Log.LogError(error);
                if (!committed) rpc.Invoke(CharacterRpc.WorldActionResult, requestId, false, error.Message, new ZPackage());
            }
        }

        private static void Apply(WorldActionTransaction action, JObject request)
        {
            string kind = (string)request["kind"];
            if (kind == "tombstone") { ServerTombstone.Create(action); return; }
            if ((float)action.Document["profile"]["playerData"]["health"] <= 0f || action.Player.IsDead())
                throw new InvalidOperationException("Character is dead.");
            if (kind == "drop") Drop(action, request);
            else if (kind == "build") ServerBuilding.Create(action, request);
            else if (kind == "interact") NativeWorldInteraction.Apply(action, request);
            else if (kind == "trade") ServerTrading.Trade(action, request);
            else if (kind == "craft") ServerCrafting.Craft(action, request);
            else if (kind == "pickup") WorldInventoryActions.Pickup(action, request);
            else if (kind == "container") WorldInventoryActions.Container(action, request);
            else throw new InvalidDataException("Unsupported world action.");
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
