using System;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class CreatureSpawned
    {
        public int schemaVersion = 1;
        public string kind = "creature_spawned";
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observation;
        public string creatureName;
        public string creatureNetworkId;
        public string networkIdUserComponent;
        public string ownerSessionId;
        public string networkSenderSessionId;
        public string networkSenderName;
        public int zoneX;
        public int zoneY;
        public string zoneOwnerSessionId;
        public bool? networkSenderWasZoneOwner;
        public string biome;
        public bool nearbySpawnerEvaluationComplete;
        public bool vanillaSpawnEvaluationComplete;
        public bool raidEvaluationComplete;
        public bool raidInProgress;
        public string raidName;
        public Vector3? raidPosition;
        public float? raidRange;
        public bool? raidSpawnerDelayElapsed;
        public bool? creatureAllowedByRaid;
        public bool? creatureWithinRaidSpawnArea;
        public string[] compatibleVanillaMechanisms;
        public NearbyCreatureSpawnerEvidence[] nearbyCompatibleSpawners;
        public Vector3 position;
        public Vector3? velocity;
        public int? level;
        public float? health;
        public bool? tamed;
        public bool? eventCreature;

        internal static CreatureSpawned Capture(Character creature, ZDO zdo)
        {
            bool eventCreatureValue;
            var entry = new CreatureSpawned
            {
                observation = "network_object_created_locally_by_server",
                creatureName = Utils.GetPrefabName(creature.gameObject),
                creatureNetworkId = zdo.m_uid.ToString(),
                networkIdUserComponent = zdo.m_uid.UserID.ToString(),
                ownerSessionId = zdo.GetOwner().ToString(),
                position = creature.transform.position,
                velocity = creature.GetVelocity(),
                level = creature.GetLevel(),
                health = creature.GetHealth(),
                tamed = creature.IsTamed(),
                eventCreature = zdo.GetBool(ZDOVars.s_eventCreature, out eventCreatureValue)
                    ? (bool?)eventCreatureValue : null
            };
            CreatureSpawnEvidence.Capture(entry, creature.gameObject, null);
            return entry;
        }

        internal static CreatureSpawned Capture(ZDO zdo, GameObject prefab, ZNetPeer sender)
        {
            int levelValue;
            float healthValue;
            bool tamedValue;
            bool eventCreatureValue;
            var entry = new CreatureSpawned
            {
                observation = "new_network_object_received_by_server",
                creatureName = Utils.GetPrefabName(prefab),
                creatureNetworkId = zdo.m_uid.ToString(),
                networkIdUserComponent = zdo.m_uid.UserID.ToString(),
                ownerSessionId = zdo.GetOwner().ToString(),
                networkSenderSessionId = sender?.m_uid.ToString(),
                networkSenderName = sender?.m_playerName,
                position = zdo.GetPosition(),
                level = zdo.GetInt(ZDOVars.s_level, out levelValue) ? (int?)levelValue : null,
                health = zdo.GetFloat(ZDOVars.s_health, out healthValue) ? (float?)healthValue : null,
                tamed = zdo.GetBool(ZDOVars.s_tamed, out tamedValue) ? (bool?)tamedValue : null,
                eventCreature = zdo.GetBool(ZDOVars.s_eventCreature, out eventCreatureValue)
                    ? (bool?)eventCreatureValue : null
            };
            CreatureSpawnEvidence.Capture(entry, prefab, sender);
            return entry;
        }
    }
}
