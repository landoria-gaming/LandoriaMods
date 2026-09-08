using System;
using System.Collections.Generic;
using Landoria.RavenWatch.Server.EventCollection;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class PlayerDebugFlyDetection : ICheatDetection
    {
        private const string DetectionCode = "PLAYER_DEBUG_FLY_ENABLED";
        private const int ServerAnomalyConfidence = 3;
        private const int ServerAttributionConfidence = 3;
        private const int ObserverAnomalyConfidence = 3;
        private const int ObserverAttributionConfidence = 3;

        public CheatDetectionFinding[] ServerBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            foreach (Event eventToAnalyze in events)
            {
                var flight = eventToAnalyze.entry as PlayerDebugFlyServer;
                if (!IsServerFinding(eventToAnalyze, flight)) continue;
                findings.Add(CreateServerFinding(eventToAnalyze, flight));
            }
            return findings.ToArray();
        }

        public CheatDetectionFinding[] ObserverBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            foreach (Event eventToAnalyze in events)
            {
                var flight = eventToAnalyze.entry as PlayerDebugFlyObserved;
                if (!IsObserverFinding(eventToAnalyze, flight)) continue;
                findings.Add(CreateObserverFinding(eventToAnalyze, flight));
            }
            return findings.ToArray();
        }

        private static bool IsServerFinding(Event source, PlayerDebugFlyServer flight)
        {
            return flight != null && (string)source.context["source"] == "server" &&
                flight.debugFly && flight.networkSenderWasOwner &&
                flight.playerSessionId == flight.networkSenderSessionId &&
                !string.IsNullOrWhiteSpace(flight.playerNetworkId) &&
                !string.IsNullOrWhiteSpace(flight.playerSessionId);
        }

        private static bool IsObserverFinding(Event source, PlayerDebugFlyObserved flight)
        {
            return flight != null && (string)source.context["source"] == "observer" &&
                flight.debugFly && !string.IsNullOrWhiteSpace(flight.playerNetworkId) &&
                !string.IsNullOrWhiteSpace(flight.ownerSessionId);
        }

        private static CheatDetectionFinding CreateServerFinding(Event source,
            PlayerDebugFlyServer flight)
        {
            return new CheatDetectionFinding(new[] { source }, GetDetectionId(flight.playerNetworkId),
                DetectionCode, ServerAnomalyConfidence, ServerAttributionConfidence,
                flight.evidence, flight.playerName, flight.playerSessionId,
                "used debug fly mode", "The server received DebugFly=true in the player's own " +
                "network data. Vanilla reserves this mode for developer cheat controls.");
        }

        private static CheatDetectionFinding CreateObserverFinding(Event source,
            PlayerDebugFlyObserved flight)
        {
            return new CheatDetectionFinding(new[] { source }, GetDetectionId(flight.playerNetworkId),
                DetectionCode, ObserverAnomalyConfidence, ObserverAttributionConfidence,
                flight.evidence, flight.playerName, flight.ownerSessionId,
                "used debug fly mode", "Another client read DebugFly=true from the remote " +
                "player's synchronized network state. Vanilla reserves this mode for developer cheat controls.");
        }

        private static string GetDetectionId(string playerNetworkId)
        {
            return string.IsNullOrWhiteSpace(playerNetworkId)
                ? null : DetectionCode + ":" + playerNetworkId;
        }
    }
}
