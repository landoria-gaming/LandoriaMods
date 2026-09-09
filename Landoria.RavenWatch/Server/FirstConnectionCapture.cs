using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class FirstConnectionCapture
    {
        private readonly HashSet<long> reportedCharacters = new HashSet<long>();
        private static readonly int IntroKey = 438569 + ZSyncAnimation.GetHash("intro");

        internal void Capture(InventoryJournal journal, JObject call, ZNetPeer peer)
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
                // Vanilla sets this synchronized animation flag during the first-spawn introduction.
                // A new network ID alone also occurs on reconnect and must not trigger this event.
                if ((int?)Value(zdo, "integers", IntroKey) != 1) continue;
                long characterId = (long?)Value(zdo, "longs", ZDOVars.s_playerID) ?? 0L;
                if (characterId == 0 || reportedCharacters.Contains(characterId)) continue;
                foreach (JObject item in StartingInventory.Create())
                    journal.Append(packet["utc"], peer, item, "first_connection", (int)item["quantity"],
                        (string)item["prefabName"], characterId,
                        (string)Value(zdo, "strings", ZDOVars.s_playerName) ?? peer.m_playerName);
                reportedCharacters.Add(characterId);
            }
        }

        private static JToken Value(JObject zdo, string collection, int key)
            => (zdo["state"]?[collection] as JArray)?
                .FirstOrDefault(value => (int)value["keyHash"] == key)?["value"];

    }
}
