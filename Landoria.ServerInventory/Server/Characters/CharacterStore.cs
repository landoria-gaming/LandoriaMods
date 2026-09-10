using System;
using System.Globalization;
using Splatform;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Landoria.ServerInventory.Serialization;

namespace Landoria.ServerInventory.Server
{
    internal static class CharacterStore
    {
        internal static byte[] LoadOrCreate(ZNetPeer peer, string appearance)
        {
            string path = GetPath(peer);
            if (!File.Exists(path))
            {
                string json = NewCharacter(peer.m_playerName, appearance);
                CharacterJson.Validate(json);
                string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                File.Move(temporary, path);
                Network.CharacterRpc.Log.LogInfo("Created a server character with the starter inventory.");
            }
            string saved = File.ReadAllText(path);
            byte[] data = FchJsonConverter.FromJson(saved);
            peer.m_playerID = long.Parse((string)JObject.Parse(saved)["profile"]["playerId"], CultureInfo.InvariantCulture);
            return data;
        }

        internal static string GetPath(ZNetPeer peer)
        {
            string identity = peer.m_socket.GetHostName();
            if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(peer.m_playerName))
                throw new InvalidDataException("Missing account or character identity.");
            if (!PlatformUserID.TryParse(identity, out var platform))
                platform = new PlatformUserID(new Platform("Steam"), identity);
            string folder = SaveSystem.GetCharacterFolderPath(FileHelpers.FileSource.Local);
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, FilePart(platform.ToString()) + "_" + FilePart(peer.m_playerName) + ".json");
        }

        private static string FilePart(string value)
        {
            var result = new StringBuilder();
            foreach (byte character in Encoding.UTF8.GetBytes(value))
            {
                if (character >= 32 && character < 127 && character != '%' &&
                    Array.IndexOf(Path.GetInvalidFileNameChars(), (char)character) < 0)
                    result.Append((char)character);
                else result.Append('%').Append(character.ToString("X2", CultureInfo.InvariantCulture));
            }
            return result.ToString();
        }

        internal static void SaveExisting(string path, JObject character)
        {
            string json = character.ToString();
            CharacterJson.Validate(json);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            File.Replace(temporary, path, null);
        }

        private static string NewCharacter(string name, string appearance)
        {
            var profile = new JObject
            {
                ["version"] = 46, ["statCount"] = 0, ["statGroups"] = new JArray(),
                ["firstSpawn"] = true, ["worlds"] = new JArray(), ["playerName"] = name,
                ["playerId"] = Utils.GenerateUID().ToString(CultureInfo.InvariantCulture),
                ["startSeed"] = "", ["usedCheats"] = false,
                ["dateCreated"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ["hasPlayerData"] = true, ["playerData"] = StarterCharacter.PlayerData()
            };
            CharacterAppearance.Apply((JObject)profile["playerData"], appearance);
            return new JObject
            {
                ["format"] = "Landoria.ServerInventory.character", ["schemaVersion"] = 1, ["profile"] = profile
            }.ToString();
        }
    }
}
