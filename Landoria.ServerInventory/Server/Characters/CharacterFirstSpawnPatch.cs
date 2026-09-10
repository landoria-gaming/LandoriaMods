using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(ZNet), "RPC_CharacterID")]
    internal static class CharacterFirstSpawnPatch
    {
        private static void Postfix(ZNet __instance, ZRpc rpc, ZDOID characterID)
        {
            if (!__instance.IsDedicated() || characterID.IsNone() || !InventoryChangesServer.IsLoaded(rpc)) return;
            var peer = __instance.GetPeers().FirstOrDefault(candidate => candidate.m_rpc == rpc);
            if (peer == null || !peer.IsReady() || peer.m_characterID != characterID) return;
            try
            {
                string path = CharacterStore.GetPath(peer);
                var character = JObject.Parse(File.ReadAllText(path));
                var profile = character["profile"] as JObject ?? throw new InvalidDataException("Missing server profile.");
                if (profile["firstSpawn"]?.Type != JTokenType.Boolean) throw new InvalidDataException("Invalid firstSpawn value.");
                if (!(bool)profile["firstSpawn"]) return;
                // Match vanilla: the first successful spawn consumes the introduction.
                profile["firstSpawn"] = false;
                CharacterStore.SaveExisting(path, character);
                CharacterRpc.Log.LogInfo("Recorded the character's first spawn; intro will not repeat.");
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }
    }
}
