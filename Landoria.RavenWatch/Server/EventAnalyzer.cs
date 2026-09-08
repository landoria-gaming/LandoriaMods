using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Landoria.RavenWatch.Server.CheatDetection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                ReportFindings(serverFindings, observerFindings);
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

        private static void ReportFindings(CheatDetectionFinding[] serverFindings,
            CheatDetectionFinding[] observerFindings)
        {
            CheatDetectionFinding[] validServer = serverFindings.Where(IsValid).ToArray();
            CheatDetectionFinding[] validObservers = observerFindings
                .Where(IsValidObserverFinding).ToArray();
            var serverSet = new HashSet<CheatDetectionFinding>(validServer);
            foreach (IGrouping<string, CheatDetectionFinding> group in validServer
                .Concat(validObservers).GroupBy(finding => finding.detectionId,
                    StringComparer.Ordinal))
            {
                CheatDetectionFinding[] groupedFindings = group.ToArray();
                int anomalyConfidence = CalculateConfidence(groupedFindings, serverSet,
                    finding => finding.anomalyConfidence);
                int attributionConfidence = CalculateConfidence(groupedFindings, serverSet,
                    finding => finding.attributionConfidence);
                ReportDetection(groupedFindings, anomalyConfidence, attributionConfidence);
            }
        }

        private static int CalculateConfidence(CheatDetectionFinding[] findings,
            HashSet<CheatDetectionFinding> serverFindings,
            Func<CheatDetectionFinding, int> confidenceSelector)
        {
            int serverCount = 0;
            int serverTotal = 0;
            foreach (CheatDetectionFinding finding in findings)
            {
                if (serverFindings.Contains(finding))
                {
                    serverCount++;
                    serverTotal += confidenceSelector(finding);
                }
            }
            double[] observerConfidences = GetObserverConfidences(findings, serverFindings,
                confidenceSelector);
            if (serverCount == 0)
                return ScaleConfidence(observerConfidences.Average());
            int observerCount = observerConfidences.Length;
            int serverWeight = Math.Max(1, observerCount);
            double observerWeight = observerCount == 1 ? 0.5d : 1d;
            double serverAverage = (double)serverTotal / serverCount;
            double weightedObserverTotal = observerConfidences.Sum() * observerWeight;
            double totalWeight = serverWeight + observerCount * observerWeight;
            return ScaleConfidence((serverAverage * serverWeight + weightedObserverTotal) /
                totalWeight);
        }

        private static double[] GetObserverConfidences(CheatDetectionFinding[] findings,
            HashSet<CheatDetectionFinding> serverFindings,
            Func<CheatDetectionFinding, int> confidenceSelector)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var findingObservers = new HashSet<string>(StringComparer.Ordinal);
            foreach (CheatDetectionFinding finding in findings)
            {
                if (serverFindings.Contains(finding)) continue;
                findingObservers.Clear();
                foreach (Event source in finding.source)
                {
                    string observerId = GetObserverId(source);
                    if (observerId == null || !findingObservers.Add(observerId)) continue;
                    totals[observerId] = totals.TryGetValue(observerId, out int total)
                        ? total + confidenceSelector(finding) : confidenceSelector(finding);
                    counts[observerId] = counts.TryGetValue(observerId, out int count)
                        ? count + 1 : 1;
                }
            }
            return totals.Select(pair => (double)pair.Value / counts[pair.Key]).ToArray();
        }

        private static bool IsValidObserverFinding(CheatDetectionFinding finding)
        {
            return IsValid(finding) && finding.source.Any(source => GetObserverId(source) != null);
        }

        private static string GetObserverId(Event source)
        {
            if ((string)source?.context?["source"] != "observer") return null;
            string observerId = (string)source.context["senderSessionId"];
            return string.IsNullOrWhiteSpace(observerId) ? null : observerId;
        }

        private static int ScaleConfidence(double confidence)
        {
            return (int)Math.Round(confidence * 10d / 3d, MidpointRounding.AwayFromZero);
        }

        private static void ReportDetection(CheatDetectionFinding[] findings,
            int anomalyConfidence, int attributionConfidence)
        {
            CheatDetectionFinding primary = findings[0];
            if (ZNet.instance == null ||
                reportedDetectionIds.ContainsOrAdd(primary.detectionId)) return;
            string identity = string.IsNullOrWhiteSpace(primary.suspectedPlayerName)
                ? "unknown player" : primary.suspectedPlayerName;
            string explanation = DetailedExplanation(findings);
            JObject record = CheatDetectionJournal.CreateRecord(findings,
                anomalyConfidence, attributionConfidence);
            CheatDetectionJournal.Append(record);
            CheatDetectionReport report = CreatePublicReport(primary, anomalyConfidence,
                attributionConfidence, explanation, record);
            RavenWatchApi.Publish(report);
            if (!report.SuppressChat)
                foreach (ZNetPeer peer in ZNet.instance.GetPeers()
                    .Where(candidate => candidate.IsReady()))
                    peer.m_rpc.Invoke(RavenWatchProtocol.AnomalyRpc, identity, primary.anomaly);
            RavenWatchPlugin.Log.LogWarning("Suspected cheating: " + identity + " " +
                primary.anomaly + ". Anomaly confidence: " + anomalyConfidence +
                "/10. Attribution confidence: " + attributionConfidence + "/10. " + explanation);
        }

        private static CheatDetectionReport CreatePublicReport(CheatDetectionFinding primary,
            int anomalyConfidence, int attributionConfidence, string explanation,
            JObject record)
        {
            return new CheatDetectionReport((string)record["detectedUtc"],
                primary.detectionId, primary.detectionCode, anomalyConfidence,
                attributionConfidence,
                primary.suspectedPlayerName, primary.suspectedSessionId,
                primary.anomaly, explanation, record.ToString(Formatting.None));
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
                finding.anomalyConfidence >= 1 && finding.anomalyConfidence <= 3 &&
                finding.attributionConfidence >= 1 && finding.attributionConfidence <= 3 &&
                !string.IsNullOrWhiteSpace(finding.anomaly) &&
                !string.IsNullOrWhiteSpace(finding.explanation);
        }
    }
}
