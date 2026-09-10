using System;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Landoria.ServerInventory.Serialization;

namespace Landoria.ServerInventory.Client
{
    internal static class InitialAppearance
    {
        private sealed class Data { internal byte[] Bytes; }
        private static readonly ConditionalWeakTable<PlayerProfile, Data> profiles = new ConditionalWeakTable<PlayerProfile, Data>();
        internal static void Remember(PlayerProfile profile, byte[] bytes) => profiles.GetOrCreateValue(profile).Bytes = bytes;

        internal static string Read(PlayerProfile profile)
        {
            if (!profiles.TryGetValue(profile, out var saved) || saved.Bytes == null)
                throw new InvalidDataException("The selected character appearance is unavailable.");
            var player = new JObject();
            using (var io = new BinaryJson(saved.Bytes)) { CharacterSchema.Player(io, player); io.Finish(); }
            var appearance = new JObject();
            foreach (string key in new[] { "modelIndex", "beardItem", "hairItem", "skinColor", "hairColor" })
                appearance[key] = player[key].DeepClone();
            return appearance.ToString(Formatting.None);
        }
    }

    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.Load))]
    internal static class LoadedAppearancePatch
    {
        private static void Postfix(PlayerProfile __instance, bool __result, byte[] ___m_playerData)
        {
            if (__result) InitialAppearance.Remember(__instance, ___m_playerData);
        }
    }

    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SavePlayerData))]
    internal static class UpdatedAppearancePatch
    {
        private static void Postfix(PlayerProfile __instance, byte[] ___m_playerData)
            => InitialAppearance.Remember(__instance, ___m_playerData);
    }
}
