using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Landoria.RavenWatch.Server.CheatDetection;

namespace Landoria.RavenWatch.Server
{
    internal static class EventAnalyzer
    {
        private static readonly ICheatDetection[] detections =
        {
            new CreatureSpawnedDetection()
        };
        private static readonly BoundedOrderedSet<string> reportedDetectionIds =
            new BoundedOrderedSet<string>(ReceivedJournal.MaximumBufferedEvents,
                StringComparer.Ordinal);
        private static int analyzing;

        internal static void Analyze()
        {
            if (Interlocked.CompareExchange(ref analyzing, 1, 0) != 0) return;
            try { AnalyzeAndClean(); }
            finally { Volatile.Write(ref analyzing, 0); }
        }

        private static void AnalyzeAndClean()
        {
            IReadOnlyList<Event> events = ReceivedJournal.GetBufferedEvents();
            AnalysisJournal.Overwrite(events);
            bool succeeded = true;
            foreach (ICheatDetection detection in detections)
            {
                if (!RunServerBasedDetection(detection, events,
                    out CheatDetectionFinding[] serverFindings)) succeeded = false;
                if (!RunObserverBasedDetection(detection, events,
                    out CheatDetectionFinding[] observerFindings)) succeeded = false;
                ReportFindings(serverFindings.Concat(observerFindings));
            }
            if (succeeded) ReceivedJournal.ArchiveOverflow();
        }

        private static bool RunServerBasedDetection(
            ICheatDetection detection, IReadOnlyList<Event> events,
            out CheatDetectionFinding[] findings)
        {
            findings = Array.Empty<CheatDetectionFinding>();
            try
            {
                findings = detection.ServerBasedDetection(events) ?? findings;
                return true;
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError(exception);
                return false;
            }
        }

        private static bool RunObserverBasedDetection(
            ICheatDetection detection, IReadOnlyList<Event> events,
            out CheatDetectionFinding[] findings)
        {
            findings = Array.Empty<CheatDetectionFinding>();
            try
            {
                findings = detection.ObserverBasedDetection(events) ?? findings;
                return true;
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError(exception);
                return false;
            }
        }

        private static void ReportFindings(IEnumerable<CheatDetectionFinding> findings)
        {
            if (findings == null) return;
            foreach (IGrouping<string, CheatDetectionFinding> group in findings
                .Where(IsValid).GroupBy(finding => finding.detectionId, StringComparer.Ordinal))
                ReportDetection(group.ToArray());
        }

        private static void ReportDetection(CheatDetectionFinding[] findings)
        {
            CheatDetectionFinding primary = findings[0];
            if (ZNet.instance == null ||
                reportedDetectionIds.ContainsOrAdd(primary.detectionId)) return;
            string identity = string.IsNullOrWhiteSpace(primary.suspectedPlayerName)
                ? "unknown player" : primary.suspectedPlayerName;
            CheatDetectionJournal.Append(findings);
            foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(candidate => candidate.IsReady()))
                peer.m_rpc.Invoke(RavenWatchProtocol.AnomalyRpc, identity, primary.anomaly);
            RavenWatchPlugin.Log.LogWarning("Suspected cheating: " + identity + " " +
                primary.anomaly + ". " + DetailedExplanation(findings));
        }

        private static string DetailedExplanation(CheatDetectionFinding[] findings)
        {
            return string.Join(" ", findings.Select(finding => finding.explanation)
                .Where(explanation => !string.IsNullOrWhiteSpace(explanation))
                .Distinct(StringComparer.Ordinal));
        }

        private static bool IsValid(CheatDetectionFinding finding)
        {
            return finding != null && finding.source != null && finding.source.Length != 0 &&
                finding.source.All(source => source?.context != null && source.entry != null) &&
                !string.IsNullOrWhiteSpace(finding.detectionId) &&
                !string.IsNullOrWhiteSpace(finding.detectionCode) &&
                !string.IsNullOrWhiteSpace(finding.anomaly) &&
                !string.IsNullOrWhiteSpace(finding.explanation);
        }
    }
}
