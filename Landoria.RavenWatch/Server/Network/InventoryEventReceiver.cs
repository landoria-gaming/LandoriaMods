using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Landoria.RavenWatch.Shared;
using Landoria.RavenWatch.Server.Inventory;
using Landoria.RavenWatch.Server.Journal;

namespace Landoria.RavenWatch.Server.Network
{
    internal static class InventoryEventReceiver
    {
        internal static ZNetPeer Peer(ZRpc rpc, long id)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc);
            var zdo = peer == null ? null : ZDOMan.instance?.GetZDO(peer.m_characterID);
            if (peer == null || !peer.IsReady() || id == 0 || zdo?.GetLong(ZDOVars.s_playerID, 0L) != id)
                throw new InvalidDataException("Inventory character does not match its connection.");
            return peer;
        }

        internal static JObject Read(ZPackage package)
        {
            if (package.Size() > InventoryWire.MaximumBytes) throw new InvalidDataException("Inventory event too large.");
            var entry = JObject.Parse(package.ReadString());
            if (package.GetPos() != package.Size() || entry["items"] != null || entry["status"] != null
                || !Guid.TryParse((string)entry["eventId"], out _) || !Guid.TryParse((string)entry["streamId"], out _)
                || (long?)entry["sequence"] < 1 || entry["sequence"] == null)
                throw new InvalidDataException("Invalid inventory event envelope.");
            var fields = new[] { "eventId", "clientCapturedUtc", "streamId", "sequence", "context",
                "operation", "changes", "craftedItem", "upgrade", "craftCount", "upgrader", "brokenItem", "destroyed" };
            foreach (var property in entry.Properties().Where(p => !fields.Contains(p.Name)).ToList()) property.Remove();
            entry["contextSource"] = "client_reported";
            ValidateContext(entry);
            return entry;
        }

        private static void ValidateContext(JObject entry)
        {
            var allowed = new[] { "items_entered", "items_exited", "items_changed", "item_crafted", "item_broken" };
            if (!allowed.Contains((string)entry["context"]))
                throw new InvalidDataException("Invalid inventory event context.");
            if ((string)entry["context"] == "item_crafted")
            {
                ValidateCraft(entry);
                return;
            }
            if (!(entry["changes"] is JArray changes) || changes.Count > InventoryWire.MaximumItems * 2)
                throw new InvalidDataException("Invalid inventory event context.");
            foreach (JObject item in changes)
            {
                if (item["prefabHash"] == null || (int?)item["quality"] <= 0 || item["quality"] == null
                    || item["variant"] == null || item["worldLevel"] == null
                    || item["quantityDelta"] == null || Math.Abs((long)item["quantityDelta"]) > int.MaxValue)
                    throw new InvalidDataException("Invalid inventory event quantity or item.");
                item["prefabName"] = ZNetScene.instance?.GetPrefab((int)item["prefabHash"])?.name;
            }
            if ((string)entry["context"] == "item_broken") ValidateBreak(entry, changes);
            else if (changes.Count == 0) throw new InvalidDataException("Empty inventory movement.");
        }

        private static void ValidateBreak(JObject entry, JArray changes)
        {
            var item = entry["brokenItem"] as JObject;
            if (item == null || item["durability"] == null || (float)item["durability"] > 0
                || (bool?)item["equipped"] != false || (int?)item["quantity"] <= 0
                || item["quantity"] == null || entry["destroyed"]?.Type != JTokenType.Boolean)
                throw new InvalidDataException("Invalid broken item.");
            bool destroyed = (bool)entry["destroyed"];
            if (changes.Count != (destroyed ? 1 : 0) || (destroyed
                && (InventoryEventDeltas.Key(changes[0]) != InventoryEventDeltas.Key(item)
                    || (long)changes[0]["quantityDelta"] != -(long)item["quantity"])))
                throw new InvalidDataException("Broken item does not match its movement.");
        }

        private static void ValidateCraft(JObject entry)
        {
            var item = entry["craftedItem"] as JObject;
            if (entry["changes"] != null || item == null || item["prefabHash"] == null
                || (int?)item["quantity"] <= 0 || item["quantity"] == null
                || (int?)item["quality"] <= 0 || item["quality"] == null
                || item["variant"] == null || item["worldLevel"] == null
                || (int?)entry["craftCount"] <= 0 || entry["craftCount"] == null
                || (int)entry["craftCount"] > (int)item["quantity"]
                || entry["upgrade"]?.Type != JTokenType.Boolean || entry["upgrader"]?.Type != JTokenType.Boolean)
                throw new InvalidDataException("Invalid crafted item context.");
            item["prefabName"] = ZNetScene.instance?.GetPrefab((int)item["prefabHash"])?.name;
        }

        internal static void Receive(ZRpc rpc, long id, ZPackage package)
        {
            if (!RpcCapture.Enabled) return;
            try
            {
                var peer = Peer(rpc, id);
                var entry = Read(package);
                if (!InventoryPollServer.Accept(rpc, id, entry)) return;
                InventoryCapture.ClientInventory(peer, entry);
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }


    }
}
