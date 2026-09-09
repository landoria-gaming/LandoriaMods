using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server
{
    internal sealed class FirstConnectionCapture
    {
        private readonly HashSet<long> reportedCharacters = new HashSet<long>();
        private static readonly int IntroKey = 438569 + ZSyncAnimation.GetHash("intro");

        internal void Capture(RpcJournal journal, JObject call, ZNetPeer peer)
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
                journal.Append(Event(call, packet, peer, zdo, characterId, objects));
                reportedCharacters.Add(characterId);
            }
        }

        private static JToken Value(JObject zdo, string collection, int key)
            => (zdo["state"]?[collection] as JArray)?
                .FirstOrDefault(value => (int)value["keyHash"] == key)?["value"];

        private static JObject Event(JObject call, JToken packet, ZNetPeer peer,
            JObject zdo, long characterId, JArray objects)
        {
            var entry = CharacterIdentity.Add(new JObject
            {
                ["event"] = "first_connection", ["callId"] = call["callId"].DeepClone(),
                ["utc"] = packet["utc"].DeepClone(), ["direction"] = "client_to_server",
                ["peer"] = packet["peer"].DeepClone(), ["rpc"] = "ZDOData",
                ["targetZdo"] = zdo["id"].DeepClone(), ["zdo"] = zdo.DeepClone(),
                ["snapshotTiming"] = "incoming_zdo_before_server_processing",
                ["classification"] = "inferred_vanilla_first_spawn",
                ["reason"] = "The character is playing the first-spawn introduction. This indicates a new character's first arrival with an unmodified client, not simply its first visit to this server.",
                ["intro"] = true, ["introKeyHash"] = IntroKey,
                ["inventory"] = StartingInventory.Create(),
                ["inventorySource"] = "assumed_starting_inventory"
            }, peer);
            // The incoming character may not exist in the server's ZDO store yet.
            entry["characterId"] = characterId.ToString(CultureInfo.InvariantCulture);
            entry["playerName"] = (string)Value(zdo, "strings", ZDOVars.s_playerName) ?? peer.m_playerName;
            entry["characterIdentityStatus"] = "resolved";
            entry["characterZdo"] = zdo["id"].DeepClone();
            entry["zdo"]["prefabName"] = "Player";
            entry["valkyrieZdo"] = NearbyValkyrie(objects, zdo, peer)?.DeepClone();
            return entry;
        }

        private static JObject NearbyValkyrie(JArray objects, JObject player, ZNetPeer peer)
        {
            foreach (JObject candidate in objects)
            {
                if ((long)candidate["owner"] != peer.m_uid) continue;
                var prefab = ZNetScene.instance?.GetPrefab((int)candidate["state"]["prefabHash"]);
                if (prefab == null || prefab.GetComponent<Valkyrie>() == null) continue;
                if (Vector3.Distance(Position(candidate), Position(player)) <= 10f) return candidate;
            }
            return null;
        }

        private static Vector3 Position(JObject zdo) => new Vector3(
            (float)zdo["position"]["x"], (float)zdo["position"]["y"], (float)zdo["position"]["z"]);
    }
}
