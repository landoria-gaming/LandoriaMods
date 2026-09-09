using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class CharacterJournalCapture
    {
        internal static void Capture(InventoryJournal journal, JObject call, ZNetPeer peer)
        {
            var packet = call["request"];
            if (peer == null || !peer.IsReady()) return;
            if ((string)packet?["direction"] != "client_to_server"
                || (string)packet?["data"]?["name"] != "ZDOData") return;
            var objects = packet["data"]["arguments"]?["pkg"]?["objects"] as JArray;
            if (objects == null) return;
            foreach (JObject zdo in objects)
            {
                if ((long)zdo["id"]["userId"] != peer.m_uid || (long)zdo["owner"] != peer.m_uid) continue;
                var prefab = ZNetScene.instance?.GetPrefab((int)zdo["state"]["prefabHash"]);
                if (prefab == null || prefab.GetComponent<Player>() == null) continue;
                long characterId = (long?)Value(zdo, "longs", ZDOVars.s_playerID) ?? 0L;
                if (characterId == 0) continue;
                journal.EnsureCharacter(packet["utc"], peer, characterId,
                    (string)Value(zdo, "strings", ZDOVars.s_playerName) ?? peer.m_playerName,
                    EventLocation.Read(zdo["position"]));
            }
        }

        private static JToken Value(JObject zdo, string collection, int key)
            => (zdo["state"]?[collection] as JArray)?
                .FirstOrDefault(value => (int)value["keyHash"] == key)?["value"];

    }
}
