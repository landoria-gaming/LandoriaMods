using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class NearbyCreatureSpawnerEvidence
    {
        public string mechanism;
        public string spawnerPrefab;
        public string spawnerNetworkId;
        public Vector3 position;
        public float distance;
    }

    internal static class CreatureSpawnEvidence
    {
        private const float NearbySearchRadius = 160f;

        internal static void Capture(CreatureSpawned entry, GameObject creaturePrefab, ZNetPeer sender)
        {
            if (entry == null) return;
            entry.compatibleVanillaMechanisms = Array.Empty<string>();
            entry.nearbyCompatibleSpawners = Array.Empty<NearbyCreatureSpawnerEvidence>();
            if (!creaturePrefab || ZDOMan.instance == null ||
                ZNetScene.instance == null || ZoneSystem.instance == null) return;
            try
            {
                Vector2i zone = ZoneSystem.GetZone(entry.position);
                entry.zoneX = zone.x;
                entry.zoneY = zone.y;
                CaptureZoneOwner(entry, zone, sender);
                entry.nearbyCompatibleSpawners = FindNearbySpawners(entry.position, creaturePrefab);
                entry.nearbySpawnerEvaluationComplete = true;
                string[] naturalSpawns = FindCompatibleNaturalSpawns(
                    entry.position, creaturePrefab, out bool completed, out string biome);
                entry.vanillaSpawnEvaluationComplete = completed;
                entry.biome = biome;
                string raid = FindCompatibleRaid(entry, creaturePrefab,
                    WorldGenerator.instance == null ? Heightmap.Biome.None :
                    WorldGenerator.instance.GetBiome(entry.position));
                entry.compatibleVanillaMechanisms = string.IsNullOrEmpty(raid)
                    ? naturalSpawns : naturalSpawns.Concat(new[] { raid }).ToArray();
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log?.LogError("Could not collect creature spawn evidence: " + exception);
            }
        }

        private static void CaptureZoneOwner(CreatureSpawned entry, Vector2i zone, ZNetPeer sender)
        {
            GameObject zoneController = ZoneSystem.instance.m_zoneCtrlPrefab;
            if (!zoneController) return;
            int zoneControllerHash = ZNetScene.instance.GetPrefabHash(zoneController);
            var objects = new List<ZDO>();
            ZDOMan.instance.FindSectorObjects(zone, 0, 0, objects);
            ZDO controller = objects.FirstOrDefault(zdo => zdo.GetPrefab() == zoneControllerHash);
            if (controller == null || !controller.HasOwner()) return;
            long owner = controller.GetOwner();
            entry.zoneOwnerSessionId = owner.ToString();
            if (sender != null) entry.networkSenderWasZoneOwner = sender.m_uid == owner;
        }

        private static NearbyCreatureSpawnerEvidence[] FindNearbySpawners(
            Vector3 spawnPosition, GameObject creaturePrefab)
        {
            int area = Mathf.CeilToInt(NearbySearchRadius / ZoneSystem.instance.m_zoneSize);
            var objects = new List<ZDO>();
            ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(spawnPosition), area, 0, objects);
            var evidence = new List<NearbyCreatureSpawnerEvidence>();
            foreach (ZDO zdo in objects)
            {
                float distance = Utils.DistanceXZ(spawnPosition, zdo.GetPosition());
                if (distance > NearbySearchRadius) continue;
                GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                if (!prefab) continue;
                foreach (string mechanism in CompatibleSpawnerMechanisms(prefab, creaturePrefab, distance))
                {
                    evidence.Add(new NearbyCreatureSpawnerEvidence
                    {
                        mechanism = mechanism,
                        spawnerPrefab = Utils.GetPrefabName(prefab),
                        spawnerNetworkId = zdo.m_uid.ToString(),
                        position = zdo.GetPosition(),
                        distance = distance
                    });
                    if (evidence.Count == 16) return evidence.ToArray();
                }
            }
            return evidence.ToArray();
        }

        private static IEnumerable<string> CompatibleSpawnerMechanisms(
            GameObject prefab, GameObject creaturePrefab, float distance)
        {
            foreach (SpawnArea component in prefab.GetComponentsInChildren<SpawnArea>(true))
                if (distance <= component.m_spawnRadius + 5f && component.m_prefabs.Any(
                    spawn => spawn != null && SamePrefab(spawn.m_prefab, creaturePrefab)))
                    yield return nameof(SpawnArea);

            foreach (CreatureSpawner component in prefab.GetComponentsInChildren<CreatureSpawner>(true))
                if (distance <= Mathf.Max(10f, component.m_spawnGroupRadius + 5f) &&
                    SamePrefab(component.m_creaturePrefab, creaturePrefab))
                    yield return nameof(CreatureSpawner);

            foreach (TriggerSpawner component in prefab.GetComponentsInChildren<TriggerSpawner>(true))
                if (distance <= 10f && component.m_creaturePrefabs != null &&
                    component.m_creaturePrefabs.Any(candidate => SamePrefab(candidate, creaturePrefab)))
                    yield return nameof(TriggerSpawner);

            foreach (Procreation component in prefab.GetComponentsInChildren<Procreation>(true))
                if (distance <= Mathf.Max(10f, component.m_spawnOffsetMax + 5f) &&
                    (SamePrefab(component.m_offspring, creaturePrefab) ||
                        SamePrefab(component.m_noPartnerOffspring, creaturePrefab)))
                    yield return nameof(Procreation);

            foreach (Growup component in prefab.GetComponentsInChildren<Growup>(true))
                if (distance <= 10f && (SamePrefab(component.m_grownPrefab, creaturePrefab) ||
                    component.m_altGrownPrefabs != null && component.m_altGrownPrefabs.Any(
                        candidate => candidate != null && SamePrefab(candidate.m_prefab, creaturePrefab))))
                    yield return nameof(Growup);

            foreach (EggGrow component in prefab.GetComponentsInChildren<EggGrow>(true))
                if (distance <= 10f && SamePrefab(component.m_grownPrefab, creaturePrefab))
                    yield return nameof(EggGrow);

            foreach (EggHatch component in prefab.GetComponentsInChildren<EggHatch>(true))
                if (distance <= 10f && SamePrefab(component.m_spawnPrefab, creaturePrefab))
                    yield return nameof(EggHatch);

            foreach (OfferingBowl component in prefab.GetComponentsInChildren<OfferingBowl>(true))
                if (distance <= NearbySearchRadius && SamePrefab(component.m_bossPrefab, creaturePrefab))
                    yield return nameof(OfferingBowl);
        }

        private static string[] FindCompatibleNaturalSpawns(Vector3 position,
            GameObject creaturePrefab, out bool completed, out string biomeName)
        {
            completed = false;
            biomeName = null;
            if (WorldGenerator.instance == null || !ZoneSystem.instance.m_zoneCtrlPrefab) return Array.Empty<string>();
            SpawnSystem spawnSystem = ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>();
            if (!spawnSystem || spawnSystem.m_spawnLists == null) return Array.Empty<string>();

            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(position);
            biomeName = biome.ToString();
            var mechanisms = new List<string>();
            foreach (SpawnSystemList list in spawnSystem.m_spawnLists.Where(item => item))
                AddMatchingSpawnRules(mechanisms, "world", list.m_spawners, creaturePrefab, biome);
            if (RandEventSystem.instance != null)
                AddMatchingSpawnRules(mechanisms, "event",
                    RandEventSystem.instance.GetCurrentSpawners(), creaturePrefab, biome);
            completed = true;
            return mechanisms.Distinct(StringComparer.Ordinal).ToArray();
        }

        private static string FindCompatibleRaid(CreatureSpawned entry,
            GameObject creaturePrefab, Heightmap.Biome biome)
        {
            RandEventSystem system = RandEventSystem.instance;
            if (system == null || biome == Heightmap.Biome.None)
            {
                if (entry.eventCreature != true) return null;
                entry.raidEvaluationComplete = true;
                return "raid:event-creature-marker";
            }
            entry.raidEvaluationComplete = true;
            RandomEvent raid = system.GetCurrentRandomEvent();
            if (raid == null)
            {
                entry.raidInProgress = false;
                return entry.eventCreature == true ? "raid:event-creature-marker" : null;
            }

            entry.raidInProgress = true;
            entry.raidName = raid.m_name;
            entry.raidPosition = raid.m_pos;
            entry.raidRange = raid.m_eventRange;
            entry.raidSpawnerDelayElapsed = raid.m_time > raid.m_spawnerDelay;
            List<SpawnSystem.SpawnData> rules = raid.m_spawn == null
                ? new List<SpawnSystem.SpawnData>() : raid.m_spawn.Where(rule =>
                    IsCompatibleSpawnRule(rule, creaturePrefab, biome)).ToList();
            entry.creatureAllowedByRaid = rules.Count > 0;
            float possibleRange = raid.m_eventRange + MaximumSpawnRange(rules);
            entry.creatureWithinRaidSpawnArea = Utils.DistanceXZ(entry.position, raid.m_pos) <= possibleRange;
            if (entry.eventCreature == true) return "raid:event-creature-marker";
            return entry.raidSpawnerDelayElapsed == true && entry.creatureAllowedByRaid == true &&
                entry.creatureWithinRaidSpawnArea == true ? "raid:" + raid.m_name : null;
        }

        private static float MaximumSpawnRange(IEnumerable<SpawnSystem.SpawnData> rules)
        {
            float maximum = 0f;
            foreach (SpawnSystem.SpawnData rule in rules)
            {
                float radius = rule.m_spawnRadiusMax > 0f ? rule.m_spawnRadiusMax : 80f;
                maximum = Mathf.Max(maximum, radius + Mathf.Max(0f, rule.m_groupRadius));
            }
            return maximum;
        }

        private static void AddMatchingSpawnRules(List<string> matches, string source,
            IEnumerable<SpawnSystem.SpawnData> rules, GameObject creaturePrefab, Heightmap.Biome biome)
        {
            if (rules == null) return;
            foreach (SpawnSystem.SpawnData rule in rules)
            {
                if (!IsCompatibleSpawnRule(rule, creaturePrefab, biome)) continue;
                matches.Add(source + ":" + (string.IsNullOrWhiteSpace(rule.m_name)
                    ? Utils.GetPrefabName(rule.m_prefab) : rule.m_name));
            }
        }

        private static bool IsCompatibleSpawnRule(SpawnSystem.SpawnData rule,
            GameObject creaturePrefab, Heightmap.Biome biome)
        {
            return rule != null && rule.m_enabled && !rule.m_devDisabled &&
                SamePrefab(rule.m_prefab, creaturePrefab) &&
                (rule.m_biome & biome) != Heightmap.Biome.None &&
                (string.IsNullOrEmpty(rule.m_requiredGlobalKey) ||
                    ZoneSystem.instance.GetGlobalKey(rule.m_requiredGlobalKey)) &&
                (rule.m_spawnAtDay || !EnvMan.IsDay()) &&
                (rule.m_spawnAtNight || !EnvMan.IsNight()) &&
                (rule.m_requiredEnvironments == null || rule.m_requiredEnvironments.Count == 0 ||
                    EnvMan.instance != null && EnvMan.instance.IsEnvironment(rule.m_requiredEnvironments));
        }

        private static bool SamePrefab(GameObject left, GameObject right)
        {
            return left && right && string.Equals(Utils.GetPrefabName(left),
                Utils.GetPrefabName(right), StringComparison.Ordinal);
        }
    }
}
