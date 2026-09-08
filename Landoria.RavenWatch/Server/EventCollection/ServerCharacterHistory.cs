using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal static class ServerCharacterHistory
    {
        private static readonly Dictionary<long, DateTime> firstSeenUtc = new();
        private static string path;

        internal static void Open(string directory)
        {
            path = Path.Combine(directory, "character-history.json");
            firstSeenUtc.Clear();
            try
            {
                if (!File.Exists(path)) return;
                JObject stored = JObject.Parse(File.ReadAllText(path));
                foreach (JProperty property in stored.Properties())
                    if (long.TryParse(property.Name, out long id) &&
                        DateTime.TryParse((string)property.Value, out DateTime seen))
                        firstSeenUtc[id] = seen.ToUniversalTime();
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }

        internal static float? Observe(ZNetPeer peer)
        {
            long? characterId = GetCharacterId(peer);
            if (!characterId.HasValue) return null;
            if (!firstSeenUtc.TryGetValue(characterId.Value, out DateTime firstSeen))
            {
                firstSeen = DateTime.UtcNow;
                firstSeenUtc.Add(characterId.Value, firstSeen);
                Save();
            }
            return (float)Math.Max(0d, (DateTime.UtcNow - firstSeen).TotalSeconds);
        }

        internal static string FirstSeenUtc(ZNetPeer peer)
        {
            long? characterId = GetCharacterId(peer);
            return characterId.HasValue && firstSeenUtc.TryGetValue(characterId.Value,
                out DateTime firstSeen) ? firstSeen.ToString("O") : null;
        }

        private static long? GetCharacterId(ZNetPeer peer)
        {
            if (peer == null || peer.m_characterID.IsNone() || ZDOMan.instance == null) return null;
            ZDO character = ZDOMan.instance.GetZDO(peer.m_characterID);
            long id = character?.GetLong(ZDOVars.s_playerID, 0L) ?? 0L;
            return id == 0L ? (long?)null : id;
        }

        private static void Save()
        {
            try
            {
                var stored = new JObject(firstSeenUtc.Select(pair =>
                    new JProperty(pair.Key.ToString(), pair.Value.ToString("O"))));
                File.WriteAllText(path, stored.ToString(Formatting.Indented));
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }

        internal static void Close()
        {
            firstSeenUtc.Clear();
            path = null;
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_CharacterID")]
    internal static class ServerCharacterSeenPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZRpc rpc)
        {
            ZNetPeer peer = ZNet.instance?.GetPeers().FirstOrDefault(candidate =>
                candidate.m_rpc == rpc);
            ServerCharacterHistory.Observe(peer);
        }
    }
}
