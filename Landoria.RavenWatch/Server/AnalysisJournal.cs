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
                using (new FileStream(journalPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }
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
                using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                    foreach (Event eventToWrite in events)
                        writer.WriteLine(CreateRecord(eventToWrite).ToString(Formatting.None));
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
