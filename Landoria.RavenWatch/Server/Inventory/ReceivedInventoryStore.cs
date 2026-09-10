using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class ReceivedInventoryStore
    {
        internal static void Save(string directory, JObject report)
        {
            if ((string)report["status"] != "received" || !(report["items"] is JArray))
                throw new InvalidDataException("Only a validated inventory report can be saved.");
            string current = Path.Combine(directory, "inventory.last.json");
            string previous = Path.Combine(directory, "inventory.previous.json");
            RenameLegacy(directory, "inventory.json", current);
            RenameLegacy(directory, "inventory.old.json", previous);
            string temporary = Path.Combine(directory, "inventory.tmp.json");
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(report.ToString(Formatting.Indented));
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Replace also overwrites an existing backup, retaining exactly the previous report.
            if (File.Exists(current)) File.Replace(temporary, current, previous);
            else
            {
                File.Move(temporary, current);
                if (File.Exists(previous)) File.Delete(previous);
            }
        }

        private static void RenameLegacy(string directory, string name, string destination)
        {
            string source = Path.Combine(directory, name);
            if (File.Exists(source) && !File.Exists(destination)) File.Move(source, destination);
        }
    }
}
