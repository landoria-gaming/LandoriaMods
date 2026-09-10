using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class PlayerDiscoveriesServer
    {
        internal static void Receive(ZRpc rpc, string json)
        {
            var peer = ZNet.instance.GetPeers().FirstOrDefault(value => value.m_rpc == rpc && value.IsReady());
            if (peer == null || !InventoryChangesServer.IsLoaded(rpc)) return;
            try
            {
                if (json == null || json.Length > 262144)
                    throw new InvalidDataException("Invalid discovery report size.");
                var report = JObject.Parse(json);
                string path = CharacterStore.GetPath(peer);
                var document = JObject.Parse(File.ReadAllText(path));
                var data = (JObject)document["profile"]["playerData"];
                var before = data.DeepClone();
                MergeNames(data, report, "knownRecipes");
                MergeNames(data, report, "knownMaterial");
                MergeStations(data, report);
                if (!JToken.DeepEquals(before, data)) CharacterStore.SaveExisting(path, document);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }

        private static JArray ReadList(JObject report, string key)
        {
            if (!(report[key] is JArray values) || values.Count > 4096)
                throw new InvalidDataException("Invalid discovery list: " + key);
            return values;
        }

        private static string ReadName(JToken token)
        {
            if (token == null || token.Type != JTokenType.String ||
                string.IsNullOrWhiteSpace((string)token) || ((string)token).Length > 256)
                throw new InvalidDataException("Invalid discovery name.");
            return (string)token;
        }

        private static void MergeNames(JObject data, JObject report, string key)
        {
            var saved = (JArray)data[key];
            var names = saved.Select(entry => ReadName(entry["value"])).ToHashSet(StringComparer.Ordinal);
            foreach (var token in ReadList(report, key))
            {
                string name = ReadName(token);
                if (names.Add(name)) saved.Add(new JObject { ["value"] = name });
            }
            if (saved.Count > 4096) throw new InvalidDataException("Too many saved discoveries.");
        }

        private static void MergeStations(JObject data, JObject report)
        {
            var saved = (JArray)data["knownStations"];
            foreach (var token in ReadList(report, "knownStations"))
            {
                if (!(token is JObject station) || station["value"]?.Type != JTokenType.Integer)
                    throw new InvalidDataException("Invalid discovered station.");
                string name = ReadName(station["key"]);
                int level = checked((int)station["value"]);
                if (level < 1 || level > 10000) throw new InvalidDataException("Invalid station level.");
                var existing = saved.OfType<JObject>().FirstOrDefault(value => (string)value["key"] == name);
                if (existing == null) saved.Add(new JObject { ["key"] = name, ["value"] = level });
                else existing["value"] = Math.Max((int)existing["value"], level);
            }
            if (saved.Count > 4096) throw new InvalidDataException("Too many saved stations.");
        }
    }
}
