using System;
using System.Linq;
namespace Landoria.RavenWatch.Server
{
    internal static class CheatDetectionReporter
    {
        private static readonly BoundedOrderedSet<string> reportedDetectionIds =
            new BoundedOrderedSet<string>(ReceivedJournal.MaximumBufferedEvents,
                StringComparer.Ordinal);

        internal static void Report(Event source, string detectionId, string detectionCode, string evidence,
            string suspectedPlayerName, string suspectedSessionId, string anomaly,
            string explanation)
        {
            if (source == null || string.IsNullOrWhiteSpace(detectionId) ||
                string.IsNullOrWhiteSpace(detectionCode) ||
                string.IsNullOrWhiteSpace(anomaly) || string.IsNullOrWhiteSpace(explanation) ||
                ZNet.instance == null) return;
            if (reportedDetectionIds.ContainsOrAdd(detectionId)) return;
            string identity = string.IsNullOrWhiteSpace(suspectedPlayerName)
                ? "unknown player" : suspectedPlayerName;
            CheatDetectionJournal.Append(source, detectionId, detectionCode, evidence, identity,
                suspectedSessionId, anomaly, explanation);
            foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(candidate => candidate.IsReady()))
                peer.m_rpc.Invoke(RavenWatchProtocol.AnomalyRpc, identity, anomaly);
            RavenWatchPlugin.Log.LogWarning("Suspected cheating: " + identity + " " + anomaly +
                ". " + explanation);
        }
    }
}
