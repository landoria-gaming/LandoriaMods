using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Journal.Decoding
{
    internal sealed class RpcDefinitions
    {
        internal readonly JObject Types;
        internal readonly JObject Packages;
        internal readonly JObject BinaryValues;
        internal readonly string GameVersion;
        private readonly Dictionary<string, List<JObject>> methods;

        internal RpcDefinitions()
        {
            const string resource = "Landoria.RavenWatch.Resources.valheim-1.0.7-rpc.json";
            using (var stream = typeof(RpcDefinitions).Assembly.GetManifestResourceStream(resource))
            using (var reader = new StreamReader(stream ?? throw new InvalidDataException("Missing RPC definitions.")))
            {
                var document = JObject.Parse(reader.ReadToEnd());
                Types = (JObject)document["wireFormat"]["types"];
                Packages = (JObject)document["wireFormat"]["packages"];
                BinaryValues = (JObject)document["wireFormat"]["zdoBinaryValues"];
                GameVersion = (string)document["gameVersion"];
                methods = ((JArray)document["rpcs"]).Cast<JObject>()
                    .GroupBy(d => (string)d["transport"] + ":" + (string)d["hash"])
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        internal List<JObject> Candidates(string transport, int hash, string[] components)
        {
            if (!methods.TryGetValue(transport + ":" + hash, out var candidates))
                throw new InvalidDataException($"Unknown {transport} RPC hash {hash}.");
            if (transport == "object" && components != null)
            {
                var matched = candidates.Where(d => components.Contains(Path.GetFileNameWithoutExtension((string)d["source"]))).ToList();
                if (matched.Count != 0) candidates = matched;
            }
            return candidates.GroupBy(d => string.Join(",", ((JArray)d["parameters"]).Select(p => (string)p["type"])))
                .Select(g => g.First()).ToList();
        }
    }
}
