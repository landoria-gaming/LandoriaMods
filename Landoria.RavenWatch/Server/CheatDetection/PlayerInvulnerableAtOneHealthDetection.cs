using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.RavenWatch.Server.EventCollection;
using UnityEngine;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class PlayerInvulnerableAtOneHealthDetection : ICheatDetection
    {
        private const string DetectionCode = "PLAYER_INVULNERABLE_AT_ONE_HEALTH";
        private const int AnomalyConfidence = 3;
        private const int AttributionConfidence = 3;

        public CheatDetectionFinding[] ServerBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            foreach (Event eventToAnalyze in events)
            {
                var damage = eventToAnalyze.entry as PlayerDamagedAtOneHealthServer;
                if (!IsCandidate(eventToAnalyze, damage) || !StillAliveAtOneHealth(damage))
                    continue;
                findings.Add(CreateFinding(eventToAnalyze, damage));
            }
            return findings.ToArray();
        }

        public CheatDetectionFinding[] ObserverBasedDetection(IReadOnlyList<Event> events)
        {
            return Array.Empty<CheatDetectionFinding>();
        }

        private static bool IsCandidate(Event source, PlayerDamagedAtOneHealthServer damage)
        {
            return damage != null && (string)source.context["source"] == "server" &&
                Mathf.Approximately(damage.healthBeforeDamage, 1f) &&
                damage.reportedDamage > 0f &&
                !string.IsNullOrWhiteSpace(damage.playerNetworkId) &&
                !string.IsNullOrWhiteSpace(damage.playerSessionId);
        }

        private static bool StillAliveAtOneHealth(PlayerDamagedAtOneHealthServer damage)
        {
            if (ZNet.instance == null || ZDOMan.instance == null) return false;
            ZNetPeer peer = ZNet.instance.GetPeers().FirstOrDefault(candidate =>
                candidate.m_uid.ToString() == damage.playerSessionId && candidate.IsReady());
            if (peer == null || peer.m_characterID.ToString() != damage.playerNetworkId) return false;
            ZDO zdo = ZDOMan.instance.GetZDO(peer.m_characterID);
            return zdo != null && zdo.GetFloat(ZDOVars.s_health, out float health) &&
                Mathf.Approximately(health, 1f);
        }

        private static CheatDetectionFinding CreateFinding(Event source,
            PlayerDamagedAtOneHealthServer damage)
        {
            string explanation = "The server routed " + damage.reportedDamage.ToString("0.##") +
                " damage reported after vanilla defenses while the player had 1 health. " +
                "At analysis time, the same character was still alive with 1 health. " +
                "Vanilla god or ghost mode clamps otherwise fatal damage to 1 health.";
            return new CheatDetectionFinding(new[] { source },
                DetectionCode + ":" + damage.playerNetworkId, DetectionCode,
                AnomalyConfidence, AttributionConfidence, damage.evidence,
                damage.playerName, damage.playerSessionId,
                "survived damage at 1 health", explanation);
        }
    }
}
