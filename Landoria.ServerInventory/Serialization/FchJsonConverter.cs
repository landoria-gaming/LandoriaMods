using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Serialization
{
    public static class FchJsonConverter
    {
        public static string ToJson(byte[] fch)
        {
            if (fch == null) throw new ArgumentNullException(nameof(fch));
            var envelope = new JObject();
            using (var io = new BinaryJson(fch))
            {
                io.Blob(envelope, "payload");
                io.Blob(envelope, "hash");
                io.Finish();
            }
            byte[] payload = Convert.FromBase64String((string)envelope["payload"]);
            using (var sha = SHA512.Create())
                if (!sha.ComputeHash(payload).SequenceEqual(Convert.FromBase64String((string)envelope["hash"])))
                    throw new InvalidDataException("Character SHA-512 checksum does not match.");
            var profile = new JObject();
            using (var io = new BinaryJson(payload)) { CharacterSchema.Profile(io, profile); io.Finish(); }
            return new JObject { ["format"] = CharacterJson.Format, ["schemaVersion"] = 1, ["profile"] = profile }
                .ToString(Formatting.Indented);
        }

        public static byte[] FromJson(string json)
        {
            var profile = CharacterJson.ReadProfile(json);
            byte[] payload;
            using (var io = new BinaryJson()) { CharacterSchema.Profile(io, profile); payload = io.Finish(); }
            var envelope = new JObject { ["payload"] = Convert.ToBase64String(payload) };
            using (var sha = SHA512.Create()) envelope["hash"] = Convert.ToBase64String(sha.ComputeHash(payload));
            using (var io = new BinaryJson())
            {
                io.Blob(envelope, "payload");
                io.Blob(envelope, "hash");
                return io.Finish();
            }
        }

    }
}
