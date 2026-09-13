using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PlayFab;
using PlayFab.MultiplayerModels;

namespace Landoria.NoServerPassword
{
    internal static class PasswordFreeServerMarker
    {
        internal const string SearchDataKey = "string_key7";
        private static readonly ConditionalWeakTable<PlayFabMatchmakingServerData, object>
            MarkedServers = new ConditionalWeakTable<PlayFabMatchmakingServerData, object>();

        internal static bool IsPresent(IReadOnlyDictionary<string, string> searchData)
        {
            return searchData != null && searchData.TryGetValue(SearchDataKey, out string value) &&
                (string.Equals(value, "TRUE", StringComparison.Ordinal) ||
                 string.Equals(value, "FALSE", StringComparison.Ordinal));
        }

        internal static void Mark(PlayFabMatchmakingServerData server)
        {
            if (server != null) MarkedServers.GetValue(server, _ => new object());
        }

        internal static bool IsMarked(PlayFabMatchmakingServerData server)
        {
            return server != null && MarkedServers.TryGetValue(server, out _);
        }
    }

    [HarmonyPatch(typeof(PlayFabMultiplayerAPI), nameof(PlayFabMultiplayerAPI.CreateLobby))]
    internal static class PublishPasswordFreeServerMarkerPatch
    {
        private static void Prefix(CreateLobbyRequest request)
        {
            if (request?.SearchData == null || ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            string value = request.SearchData[PasswordFreeServerMarker.SearchDataKey];
            request.SearchData[PasswordFreeServerMarker.SearchDataKey] = value.ToUpperInvariant();
            NoServerPasswordPlugin.Log.LogDebug("Published the password-free PlayFab marker.");
        }
    }

    [HarmonyPatch(typeof(ZPlayFabLobbySearch), "ToServerData",
        typeof(string), typeof(uint), typeof(uint), typeof(Dictionary<string, string>),
        typeof(Dictionary<string, string>), typeof(bool))]
    internal static class ReadPasswordFreeServerMarkerPatch
    {
        private static void Postfix(Dictionary<string, string> searchData,
            PlayFabMatchmakingServerData __result)
        {
            if (__result != null && PasswordFreeServerMarker.IsPresent(searchData))
            {
                PasswordFreeServerMarker.Mark(__result);
            }
        }
    }

    [HarmonyPatch(typeof(PlayFabMatchmakingServerData), nameof(PlayFabMatchmakingServerData.ToServerMatchmakingData))]
    internal static class PlayFabPasswordDisplayPatch
    {
        private static bool Prefix(PlayFabMatchmakingServerData __instance,
            DateTime timestampUtc, ref ServerMatchmakingData __result)
        {
            if (!PasswordFreeServerMarker.IsMarked(__instance)) return true;
            __result = CreateMatchmakingData(__instance, timestampUtc);
            return false;
        }

        private static ServerMatchmakingData CreateMatchmakingData(
            PlayFabMatchmakingServerData server, DateTime timestampUtc)
        {
            return new ServerMatchmakingData(timestampUtc, server.serverName,
                server.numPlayers, server.maxNumPlayers, server.platformUserID,
                server.gameVersion, server.networkVersion, server.joinCode,
                false, server.platformRestriction, server.modifiers);
        }
    }
}
