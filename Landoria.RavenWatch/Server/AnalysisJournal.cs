using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class AnalysisJournal
    {
        private static string path;

        internal static void Open(string journalPath)
        {
            try
            {
                using (var stream = new FileStream(journalPath, FileMode.CreateNew,
                    FileAccess.Write, FileShare.Read))
                {
                    byte[] emptyArray = new UTF8Encoding(false).GetBytes("[]");
                    stream.Write(emptyArray, 0, emptyArray.Length);
                }
                path = journalPath;
                RavenWatchPlugin.Log.LogInfo("Analysis memory journal: " + path);
            }
            catch (Exception exception)
            {
                path = null;
                RavenWatchPlugin.Log.LogError("Could not create the analysis memory journal: " + exception);
            }
        }

        internal static void Overwrite(IReadOnlyList<Event> events)
        {
            if (path == null) return;
            try
            {
                var records = new JArray();
                foreach (Event eventToWrite in events)
                    records.Add(CreateRecord(eventToWrite));
                File.WriteAllText(path, records.ToString(Formatting.None),
                    new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError("Could not overwrite the analysis journal: " + exception);
            }
        }

        internal static void Close() => path = null;

        private static JObject CreateRecord(Event eventToWrite)
        {
            JObject record = (JObject)eventToWrite.context.DeepClone();
            record["events"] = new JArray(JournalJson.CreateEvent(eventToWrite.entry));
            return record;
        }
    }
}
