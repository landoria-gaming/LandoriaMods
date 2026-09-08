using System;
using System.Collections.Generic;
using System.IO;
using Landoria.RavenWatch.Server.CheatDetection;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class Event
    {
        internal JObject context;
        internal object entry;
    }

    internal static class ReceivedJournal
    {
        internal const int MaximumBufferedEvents = 500;
        private const int ArchiveBatchSize = 100;
        private static readonly List<Event> pending = new List<Event>();
        private static readonly IReadOnlyList<Event> pendingView = pending.AsReadOnly();
        private static JsonArrayJournal writer;

        internal static void Open()
        {
            try
            {
                string directory = Path.Combine(
                    Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "RavenWatch");
                Directory.CreateDirectory(directory);
                string name = "received-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                    + "-" + Guid.NewGuid().ToString("N");
                string path = Path.Combine(directory, name + ".json");
                writer = new JsonArrayJournal(path);
                AnalysisJournal.Open(Path.Combine(directory, name + "-memory.json"));
                CheatDetectionJournal.Open(Path.Combine(directory, name + "-cheat-detections.json"));
                pending.Clear();
                RavenWatchPlugin.Log.LogInfo("Received event journal: " + path);
            }
            catch (Exception exception) { RavenWatchPlugin.Log.LogError(exception); }
        }

        internal static bool Append(ZNetPeer peer, string stream, long batch, IReadOnlyCollection<object> events)
        {
            if (writer == null) return false;
            JObject context = CreateContext(peer, stream, batch);
            foreach (object entry in events)
                pending.Add(new Event { context = context, entry = entry });
            return true;
        }

        internal static bool Append(Event eventToAppend)
        {
            if (writer == null || eventToAppend == null || eventToAppend.context == null ||
                eventToAppend.entry == null)
                return false;
            pending.Add(eventToAppend);
            return true;
        }

        internal static IReadOnlyList<Event> GetBufferedEvents() => pendingView;

        internal static void ArchiveOverflow()
        {
            while (pending.Count > MaximumBufferedEvents) WriteOldest(ArchiveBatchSize);
        }

        private static JObject CreateContext(ZNetPeer peer, string stream, long batch)
        {
            return new JObject
            {
                ["schemaVersion"] = 1, ["code"] = "CLIENT_EVENTS_RECEIVED",
                ["source"] = "observer",
                ["receivedUtc"] = DateTime.UtcNow.ToString("O"),
                ["worldId"] = ZNet.instance.GetWorldUID().ToString(),
                ["senderSessionId"] = peer.m_uid.ToString(), ["senderName"] = peer.m_playerName,
                ["senderCharacterNetworkId"] = peer.m_characterID.ToString(),
                ["clientStreamId"] = stream, ["batchId"] = batch
            };
        }

        private static void WriteOldest(int count)
        {
            while (count > 0 && pending.Count > 0)
            {
                JObject context = pending[0].context;
                var events = new JArray();
                foreach (Event item in pending)
                {
                    if (events.Count == count || !ReferenceEquals(item.context, context)) break;
                    events.Add(JournalJson.CreateEvent(item.entry));
                }
                JObject record = (JObject)context.DeepClone();
                record["events"] = events;
                writer.Append(record);
                pending.RemoveRange(0, events.Count);
                count -= events.Count;
            }
        }

        internal static void Close()
        {
            JsonArrayJournal closing = writer;
            try
            {
                if (closing != null) WriteOldest(pending.Count);
                pending.Clear();
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
            finally
            {
                writer = null;
                AnalysisJournal.Close();
                CheatDetectionJournal.Close();
                try { closing?.Dispose(); }
                catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
            }
        }
    }
}
