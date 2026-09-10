using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Serialization
{
    internal static class CharacterJson
    {
        internal const string Format = "Landoria.ServerInventory.character";

        internal static JObject ReadProfile(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (json.Length > BinaryJson.MaximumBytes * 4L) throw new InvalidDataException("JSON is too large.");
            JObject root;
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, MaxDepth = 64 })
            {
                root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new InvalidDataException("Trailing JSON content.");
            }
            if ((string)root["format"] != Format || (int?)root["schemaVersion"] != 1)
                throw new InvalidDataException("Unsupported JSON format.");
            return root["profile"] as JObject ?? throw new InvalidDataException("Missing profile.");
        }

        internal static void Validate(string json)
        {
            var profile = ReadProfile(json);
            // Reuse the binary schema checks, without building an FCH envelope or checksum.
            using (var io = new BinaryJson())
            {
                CharacterSchema.Profile(io, profile);
                // Keep the size limit compatible with later FCH export (two lengths and SHA-512).
                io.ValidateComplete(reservedBytes: 72);
            }
        }
    }
}
