using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Landoria.RavenWatch.Server.CheatDetection;

namespace Landoria.RavenWatch.Server
{
    internal static class CheatDetectionJournal
    {
        private static StreamWriter writer;

        internal static void Open(string path)
        {
            try
            {
                writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write,
                    FileShare.Read), new UTF8Encoding(false));
                RavenWatchPlugin.Log.LogInfo("Cheat detection journal: " + path);
            }
            catch (Exception exception)
            {
                writer = null;
                RavenWatchPlugin.Log.LogError("Could not create the cheat detection journal: " + exception);
            }
        }

        internal static void Append(CheatDetectionFinding[] findings)
        {
            if (writer == null || findings == null || findings.Length == 0) return;
            try
            {
                writer.WriteLine(CreateRecord(findings).ToString(Formatting.None));
                writer.Flush();
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError("Could not write the cheat detection journal: " + exception);
            }
        }

        private static JObject CreateRecord(CheatDetectionFinding[] findings)
        {
            CheatDetectionFinding primary = findings[0];
            return new JObject
            {
                ["schemaVersion"] = 1,
                ["code"] = "CHEAT_DETECTED",
                ["kind"] = "cheat_detected",
                ["detectedUtc"] = DateTime.UtcNow.ToString("O"),
                ["detectionId"] = primary.detectionId,
                ["detectionCode"] = primary.detectionCode,
                ["findings"] = CreateFindings(findings)
            };
        }

        private static JArray CreateFindings(CheatDetectionFinding[] findings)
        {
            var result = new JArray();
            foreach (CheatDetectionFinding finding in findings)
            {
                result.Add(new JObject
                {
                    ["detectionId"] = finding.detectionId,
                    ["detectionCode"] = finding.detectionCode,
                    ["evidence"] = finding.evidence,
                    ["playerName"] = string.IsNullOrWhiteSpace(finding.suspectedPlayerName)
                        ? "unknown player" : finding.suspectedPlayerName,
                    ["playerSessionId"] = finding.suspectedSessionId,
                    ["anomaly"] = finding.anomaly,
                    ["explanation"] = finding.explanation,
                    ["sources"] = CreateSources(finding.source)
                });
            }
            return result;
        }

        private static JArray CreateSources(Event[] source)
        {
            var sources = new JArray();
            foreach (Event item in source)
            {
                sources.Add(new JObject
                {
                    ["receiptContext"] = item.context.DeepClone(),
                    ["sourceEvent"] = JournalJson.CreateEvent(item.entry)
                });
            }
            return sources;
        }

        internal static void Close()
        {
            StreamWriter closing = writer;
            writer = null;
            try { closing?.Dispose(); }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }
}
