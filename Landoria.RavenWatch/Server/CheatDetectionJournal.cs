using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        internal static void Append(Event source, string detectionId, string detectionCode, string evidence,
            string identity, string suspectedSessionId, string anomaly, string explanation)
        {
            if (writer == null) return;
            try
            {
                writer.WriteLine(CreateRecord(source, detectionCode, evidence, identity,
                        detectionId, suspectedSessionId, anomaly, explanation)
                    .ToString(Formatting.None));
                writer.Flush();
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError("Could not write the cheat detection journal: " + exception);
            }
        }

        private static JObject CreateRecord(Event source, string detectionCode, string evidence,
            string identity, string detectionId, string suspectedSessionId, string anomaly,
            string explanation)
        {
            return new JObject
            {
                ["schemaVersion"] = 1,
                ["code"] = "CHEAT_DETECTED",
                ["kind"] = "cheat_detected",
                ["detectedUtc"] = DateTime.UtcNow.ToString("O"),
                ["detectionId"] = detectionId,
                ["detectionCode"] = detectionCode,
                ["evidence"] = evidence,
                ["playerName"] = identity,
                ["playerSessionId"] = suspectedSessionId,
                ["anomaly"] = anomaly,
                ["explanation"] = explanation,
                ["receiptContext"] = source.context.DeepClone(),
                ["sourceEvent"] = JournalJson.CreateEvent(source.entry)
            };
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
