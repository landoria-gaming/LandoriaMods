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
        private static readonly Dictionary<string, DateTime> firstSeenUtc =
            new(StringComparer.Ordinal);
        private static string path;

        internal static void Open(string historyPath)
        {
            path = historyPath;
            firstSeenUtc.Clear();
            try
            {
                if (File.Exists(path))
                {
                    JObject stored = JObject.Parse(File.ReadAllText(path));
                    foreach (JProperty property in stored.Properties())
                        if (property.Name.Contains("_") &&
                            DateTime.TryParse((string)property.Value, out DateTime seen))
                            firstSeenUtc[property.Name] = seen.ToUniversalTime();
                }
                RavenWatchPlugin.Log?.LogInfo("Character history: " + path);
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }

        internal static float? Observe(ZNetPeer peer)
        {
            string identity = GetIdentity(peer);
            if (identity == null) return null;
            if (!firstSeenUtc.TryGetValue(identity, out DateTime firstSeen))
            {
                firstSeen = DateTime.UtcNow;
                firstSeenUtc.Add(identity, firstSeen);
                Save();
            }
            return (float)Math.Max(0d, (DateTime.UtcNow - firstSeen).TotalSeconds);
        }

        internal static string FirstSeenUtc(ZNetPeer peer)
        {
            string identity = GetIdentity(peer);
            return identity != null && firstSeenUtc.TryGetValue(identity,
                out DateTime firstSeen) ? firstSeen.ToString("O") : null;
        }

        internal static string GetIdentity(ZNetPeer peer)
        {
            string account = peer?.m_socket?.GetHostName();
            if (string.IsNullOrWhiteSpace(account) ||
                string.IsNullOrWhiteSpace(peer.m_playerName)) return null;
            if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks &&
                !account.StartsWith("Steam_", StringComparison.OrdinalIgnoreCase))
                account = "Steam_" + account;
            return account + "_" + peer.m_playerName;
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
