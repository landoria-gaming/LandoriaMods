using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class DroppedItemCapture
    {
        internal static void Capture(InventoryJournal journal, JObject call, ZNetPeer peer)
        {
            var packet = call["request"];
            if (peer == null || !peer.IsReady() || ZDOMan.instance == null) return;
            if ((string)packet?["direction"] != "client_to_server" || (string)packet?["data"]?["name"] != "ZDOData") return;
            var objects = packet["data"]["arguments"]?["pkg"]?["objects"] as JArray;
            if (objects == null) return;
            var seenInPacket = new HashSet<ZDOID>();
            foreach (JObject zdo in objects)
            {
                var id = new ZDOID((long)zdo["id"]["userId"], (uint)zdo["id"]["id"]);
                // Existing server ZDOs are updates, including later movement of the dropped stack.
                if (id.UserID != peer.m_uid || (long)zdo["owner"] != peer.m_uid
                    || ZDOMan.instance.GetZDO(id) != null || !seenInPacket.Add(id)) continue;
                int hash = (int)zdo["state"]["prefabHash"];
                var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(hash) : null;
                if (prefab == null || prefab.GetComponent<ItemDrop>() == null) continue;
                JObject item = Item(zdo);
                if ((bool?)item?["pickedUp"] != true || ((int?)item?["stack"] ?? 0) < 1) continue;
                journal.Append(packet["utc"], peer, item, "drop", -(int)item["stack"], prefab.name);
            }
        }

        private static JObject Item(JObject zdo)
        {
            var values = zdo["state"]?["byteArrays"] as JArray;
            if (values == null) return null;
            foreach (var value in values)
                if ((int)value["keyHash"] == ZDOVars.s_itemData) return value["value"]?["item"] as JObject;
            return null;
        }

    }
}
