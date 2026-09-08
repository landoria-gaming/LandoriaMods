using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.RavenWatch.Server.EventCollection;
using UnityEngine;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class CreatureSpawnedDetection : ICheatDetection
    {
        private const string DetectionCode = "CREATURE_SPAWNED_WITHOUT_VANILLA_SOURCE";
        private const int ServerAnomalyConfidence = 3;
        private const int ServerAttributionConfidence = 3;
        private const int ObserverAnomalyConfidence = 3;
        private const int ObserverAttributionConfidence = 2;
        private static string GetDetectionId(CreatureSpawned creature)
        {
            return string.IsNullOrWhiteSpace(creature?.creatureNetworkId)
                ? null : DetectionCode + ":" + creature.creatureNetworkId;
        }

        public CheatDetectionFinding[] ServerBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            foreach (Event eventToAnalyze in events)
            {
                CreatureSpawned creature = GetSuspiciousCreature(eventToAnalyze);
                if (creature == null) continue;
                string detectionId = GetDetectionId(creature);
                findings.Add(new CheatDetectionFinding(new[] { eventToAnalyze }, detectionId,
                    DetectionCode, ServerAnomalyConfidence, ServerAttributionConfidence,
                    "without_compatible_vanilla_spawn_source",
                    creature.networkSenderName, creature.networkSenderSessionId,
                    "spawned a " + creature.creatureName, Explain(creature)));
            }
            return findings.ToArray();
        }

        public CheatDetectionFinding[] ObserverBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            foreach (Event observerEvent in events)
            {
                CreatureAppearanceObserved observed = GetObservedCreature(observerEvent);
                CreatureSpawned assessment = AssessObservedCreature(observed);
                if (assessment == null) continue;
                NearbyPlayerEvidence suspect = FindSuspect(observed);
                findings.Add(CreateObserverFinding(observerEvent, assessment, suspect));
            }
            return findings.ToArray();
        }

        private static CreatureSpawned AssessObservedCreature(CreatureAppearanceObserved observed)
        {
            if (observed == null || ZNetScene.instance == null) return null;
            string prefabName = NormalizePrefabName(observed.creatureName);
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab) return null;
            var assessment = new CreatureSpawned
            {
                creatureName = prefabName,
                creatureNetworkId = observed.creatureNetworkId,
                ownerSessionId = observed.ownerSessionId,
                position = observed.position
            };
            CreatureSpawnEvidence.Capture(assessment, prefab, null);
            return HasNoVanillaSource(assessment) ? assessment : null;
        }

        private static CreatureAppearanceObserved GetObservedCreature(Event eventToAnalyze)
        {
            var observed = eventToAnalyze.entry as CreatureAppearanceObserved;
            if (observed == null || (string)eventToAnalyze.context["source"] != "observer" ||
                observed.observation != "existing_network_object_loaded" ||
                string.IsNullOrWhiteSpace(observed.creatureNetworkId)) return null;
            return observed;
        }

        private static NearbyPlayerEvidence FindSuspect(CreatureAppearanceObserved observed)
        {
            return observed?.nearbyPlayers?.FirstOrDefault(player => player != null &&
                !player.isObserver && player.ownerSessionId == observed.ownerSessionId);
        }

        private static CheatDetectionFinding CreateObserverFinding(Event observerEvent,
            CreatureSpawned creature, NearbyPlayerEvidence suspect)
        {
            return new CheatDetectionFinding(new[] { observerEvent },
                GetDetectionId(creature), DetectionCode, ObserverAnomalyConfidence,
                ObserverAttributionConfidence,
                "observer_found_no_compatible_vanilla_spawn_source", suspect?.name,
                suspect?.ownerSessionId ?? creature.ownerSessionId,
                "spawned a " + creature.creatureName, Explain(creature));
        }

        private static string NormalizePrefabName(string name)
        {
            const string CloneSuffix = "(Clone)";
            if (string.IsNullOrWhiteSpace(name)) return null;
            return name.EndsWith(CloneSuffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - CloneSuffix.Length) : name;
        }

        private static CreatureSpawned GetSuspiciousCreature(Event eventToAnalyze)
        {
            CreatureSpawned creature = eventToAnalyze.entry as CreatureSpawned;
            if (creature == null || (string)eventToAnalyze.context["source"] != "server" ||
                creature.observation != "new_network_object_received_by_server" ||
                string.IsNullOrWhiteSpace(creature.creatureName) ||
                string.IsNullOrWhiteSpace(creature.creatureNetworkId) ||
                string.IsNullOrWhiteSpace(creature.networkSenderSessionId)) return null;

            // Anomaly: the server received a new creature ZDO from a client and found neither
            // a compatible vanilla spawn rule nor a nearby networked spawner capable of
            // creating that creature prefab. Zone ownership does not explain the spawn.
            return HasNoVanillaSource(creature) ? creature : null;
        }

        private static bool HasNoVanillaSource(CreatureSpawned creature)
        {
            return creature.nearbySpawnerEvaluationComplete &&
                creature.vanillaSpawnEvaluationComplete &&
                creature.raidEvaluationComplete &&
                creature.nearbyCompatibleSpawners != null &&
                creature.compatibleVanillaMechanisms != null &&
                creature.nearbyCompatibleSpawners.Length == 0 &&
                creature.compatibleVanillaMechanisms.Length == 0;
        }

        private static string Explain(CreatureSpawned creature)
        {
            string location = string.IsNullOrWhiteSpace(creature.biome)
                ? "at that location" : "in the " + creature.biome + " biome";
            return "No active vanilla spawn rule could create the " + creature.creatureName +
                " " + location + ", and no nearby compatible spawner was found. " +
                ExplainRaid(creature) + " The creature appeared without a valid in-game source.";
        }

        private static string ExplainRaid(CreatureSpawned creature)
        {
            if (!creature.raidInProgress)
                return "No raid was in progress, and the creature was not marked as created by an event.";
            string raid = string.IsNullOrWhiteSpace(creature.raidName)
                ? "The active raid" : "The " + creature.raidName + " raid";
            if (creature.raidSpawnerDelayElapsed != true)
                return raid + " had not started spawning creatures, and the creature was not marked as created by an event.";
            if (creature.creatureAllowedByRaid != true)
                return raid + " could not spawn this creature, which was not marked as created by an event.";
            return raid + " could not spawn this creature at that location, and the creature was not marked as created by an event.";
        }

    }
}
