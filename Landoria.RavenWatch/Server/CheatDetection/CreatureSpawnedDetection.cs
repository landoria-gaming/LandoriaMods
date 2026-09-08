using System;
using System.Collections.Generic;
using Landoria.RavenWatch.Server.EventCollection;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class CreatureSpawnedDetection : ICheatDetection
    {
        private const string DetectionCode = "CREATURE_SPAWNED_WITHOUT_VANILLA_SOURCE";
        public string GetDetectionId(Event eventToAnalyze)
        {
            CreatureSpawned creature = eventToAnalyze?.entry as CreatureSpawned;
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
                string detectionId = GetDetectionId(eventToAnalyze);
                findings.Add(new CheatDetectionFinding(new[] { eventToAnalyze }, detectionId,
                    DetectionCode, "without_compatible_vanilla_spawn_source",
                    creature.networkSenderName, creature.networkSenderSessionId,
                    "spawned a " + creature.creatureName, Explain(creature)));
            }
            return findings.ToArray();
        }

        public CheatDetectionFinding[] ObserverBasedDetection(IReadOnlyList<Event> events)
        {
            return Array.Empty<CheatDetectionFinding>();
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
            if (!creature.nearbySpawnerEvaluationComplete ||
                !creature.vanillaSpawnEvaluationComplete ||
                !creature.raidEvaluationComplete ||
                creature.nearbyCompatibleSpawners == null ||
                creature.compatibleVanillaMechanisms == null ||
                creature.nearbyCompatibleSpawners.Length != 0 ||
                creature.compatibleVanillaMechanisms.Length != 0) return null;
            return creature;
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
