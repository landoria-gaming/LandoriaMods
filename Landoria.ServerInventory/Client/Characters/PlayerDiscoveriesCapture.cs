using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    internal static class PlayerDiscoveriesCapture
    {
        private static ZRpc lastRpc;
        private static Player lastPlayer;
        private static string lastJson;

        internal static void Send(Player player, HashSet<string> recipes,
            HashSet<string> materials, Dictionary<string, int> stations)
        {
            if (!ClientDamageGuard.Active || player != Player.m_localPlayer) return;
            try
            {
                var rpc = ZNet.instance.GetServerRPC();
                if (rpc == null || !rpc.IsConnected()) return;
                var data = new JObject {
                    ["knownRecipes"] = new JArray(recipes.OrderBy(value => value, StringComparer.Ordinal)),
                    ["knownMaterial"] = new JArray(materials.OrderBy(value => value, StringComparer.Ordinal)),
                    ["knownStations"] = new JArray(stations.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => new JObject { ["key"] = pair.Key, ["value"] = pair.Value })) };
                string json = data.ToString(Formatting.None);
                if (rpc == lastRpc && player == lastPlayer && json == lastJson) return;
                rpc.Invoke(CharacterRpc.Discoveries, json);
                lastRpc = rpc;
                lastPlayer = player;
                lastJson = json;
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateKnownRecipesList")]
    internal static class RecipeDiscoveryPatch
    {
        private static void Postfix(Player __instance, bool ___m_isLoading, HashSet<string> ___m_knownRecipes,
            HashSet<string> ___m_knownMaterial, Dictionary<string, int> ___m_knownStations)
        {
            if (!___m_isLoading)
                PlayerDiscoveriesCapture.Send(__instance, ___m_knownRecipes, ___m_knownMaterial, ___m_knownStations);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class SpawnDiscoveriesPatch
    {
        private static void Postfix(Player __instance, HashSet<string> ___m_knownRecipes,
            HashSet<string> ___m_knownMaterial, Dictionary<string, int> ___m_knownStations)
            => PlayerDiscoveriesCapture.Send(__instance, ___m_knownRecipes, ___m_knownMaterial, ___m_knownStations);
    }
}
