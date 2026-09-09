using Landoria.RavenWatch.Server.Journal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal sealed class InventoryDropErrors
    {
        private readonly string path;
        private readonly HashSet<string> recorded = new HashSet<string>(StringComparer.Ordinal);

        internal InventoryDropErrors(string directory)
        {
            path = Path.Combine(directory, "inventory-drop-errors.json");
            if (!File.Exists(path)) return;
            using (var text = File.OpenText(path))
            using (var reader = new JsonTextReader(text))
            {
                if (!reader.Read() || reader.TokenType != JsonToken.StartArray)
                    throw new InvalidDataException("Drop errors must be a JSON array.");
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    var entry = JObject.Load(reader);
                    string id = (string)entry["dropErrorId"];
                    if (id != null) recorded.Add(id);
                }
                if (reader.TokenType != JsonToken.EndArray || reader.Read())
                    throw new InvalidDataException("Incomplete drop error journal.");
            }
        }

        internal void Record(JObject drop, long unmatchedQuantity)
        {
            string id = Identity(drop);
            if (recorded.Contains(id)) return;
            var entry = (JObject)drop.DeepClone();
            entry["dropErrorId"] = id;
            entry["unmatchedQuantity"] = unmatchedQuantity;
            entry["reason"] = "No matching inventory stock for this quantity.";
            using (var journal = RpcJournal.OpenPersistent(path)) journal.Append(entry);
            recorded.Add(id);
            RpcCapture.Log.LogWarning("Inventory drop exceeds recorded stock: "
                + (string)drop["prefabName"] + ", missing quantity " + unmatchedQuantity + ".");
        }

        private static string Identity(JObject drop)
        {
            string eventId = (string)drop["eventId"];
            if (!string.IsNullOrEmpty(eventId)) return eventId;
            // Older journals have no eventId; keep replay stable without modifying those events.
            using (var hash = SHA256.Create())
                return "legacy-" + BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(drop.ToString(Formatting.None)))).Replace("-", "");
        }
    }
}
