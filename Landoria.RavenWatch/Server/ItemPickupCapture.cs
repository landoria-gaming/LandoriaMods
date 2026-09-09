using System.Collections.Generic;
using Landoria.RavenWatch.Server.Decoding;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class ItemPickupCapture
    {
        internal static void Capture(InventoryJournal journal, JObject call, ZNetPeer peer, RpcJsonDecoder decoder)
        {
            if (peer == null || !peer.IsReady() || ZDOMan.instance == null) return;
            var packet = call["request"];
            var routed = packet?["data"]?["arguments"]?["pkg"];
            if ((string)packet?["direction"] != "client_to_server"
                || (long?)routed?["senderPeerId"] != peer.m_uid) return;
            var ids = routed?["parameters"]?["arguments"]?["pkg"]?["ids"] as JArray;
            if (ids == null) return;
            var seen = new HashSet<ZDOID>();
            foreach (var target in ids)
            {
                var id = new ZDOID((long)target["userId"], (uint)target["id"]);
                if (!seen.Add(id)) continue;
                var zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null) continue;
                var prefab = ZNetScene.instance?.GetPrefab(zdo.GetPrefab());
                // Count the resulting ItemDrop, not the Pickable that produced it.
                if (prefab == null || prefab.GetComponent<ItemDrop>() == null) continue;
                var bytes = zdo.GetByteArray(ZDOVars.s_itemData);
                if (bytes == null) continue;
                // Decode only item data; position, rotation and other ZDO values are not needed.
                var item = decoder.DecodeItemData(bytes);
                int quantity = (int)item["stack"];
                if (quantity > 0)
                    journal.Append(packet["utc"], peer, item, "pickup", quantity, prefab.name, zdo.GetPosition());
            }
        }
    }
}
