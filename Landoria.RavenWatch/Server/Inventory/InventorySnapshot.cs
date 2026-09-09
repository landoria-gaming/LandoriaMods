using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal sealed class InventorySnapshot
    {
        private readonly List<JObject> items = new List<JObject>();
        private readonly string path;
        private readonly InventoryDropErrors dropErrors;
        private static readonly string[] MatchFields =
            { "characterId", "prefabHash", "quality", "variant", "worldLevel" };

        internal InventorySnapshot(string directory)
        {
            path = Path.Combine(directory, "inventory.json");
            dropErrors = new InventoryDropErrors(directory);
            var files = Directory.EnumerateFiles(directory, "*inventory-events*.json")
                .OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);
            foreach (string file in files) Replay(file);
        }

        private void Replay(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var text = new StreamReader(stream))
            using (var reader = new JsonTextReader(text))
            {
                if (!reader.Read() || reader.TokenType != JsonToken.StartArray)
                    throw new InvalidDataException("Inventory events must be a JSON array: " + file);
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType != JsonToken.StartObject)
                        throw new InvalidDataException("Inventory event must be an object: " + file);
                    Apply(JObject.Load(reader));
                }
                if (reader.TokenType != JsonToken.EndArray || reader.Read())
                    throw new InvalidDataException("Incomplete inventory events: " + file);
            }
        }

        internal void Apply(JObject entry)
        {
            int delta = (int)entry["quantityDelta"];
            if (delta > 0)
            {
                items.Add((JObject)entry.DeepClone());
                return;
            }
            long remaining = -(long)delta;
            // Remove the oldest matching pickups first; retain their event IDs and other metadata.
            for (int i = 0; i < items.Count && remaining > 0;)
            {
                var item = items[i];
                if (!MatchFields.All(field => JToken.DeepEquals(item[field], entry[field]))) { i++; continue; }
                int quantity = (int)item["quantityDelta"];
                int removed = (int)Math.Min(remaining, quantity);
                remaining -= removed;
                if (removed == quantity) items.RemoveAt(i);
                else { item["quantityDelta"] = quantity - removed; i++; }
            }
            if (remaining > 0)
                dropErrors.Record(entry, remaining);
        }

        internal void Save()
        {
            string temporary = path + ".tmp";
            using (var text = new StreamWriter(temporary, false, new UTF8Encoding(false)))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.Indented })
            {
                writer.WriteStartArray();
                foreach (var item in items) item.WriteTo(writer);
                writer.WriteEndArray();
            }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
