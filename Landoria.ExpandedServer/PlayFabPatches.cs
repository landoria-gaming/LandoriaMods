using HarmonyLib;
using PlayFab;
using PlayFab.MultiplayerModels;
using PlayFab.Party;

namespace Landoria.ExpandedServer
{
    [HarmonyPatch(typeof(PlayFabMultiplayerAPI), nameof(PlayFabMultiplayerAPI.CreateLobby))]
    internal static class IncreasePlayFabLobbyPlayerLimitPatch
    {
        private static void Prefix(CreateLobbyRequest request)
        {
            if (request != null && ExpandedServerPlugin.IsLocalServer &&
                request.MaxPlayers != ExpandedServerPlugin.PlayFabCapacity)
            {
                request.MaxPlayers = ExpandedServerPlugin.PlayFabCapacity;
                ExpandedServerPlugin.Log.LogDebug(
                    $"PlayFab lobby capacity set to {request.MaxPlayers} network members " +
                    $"for {ExpandedServerPlugin.MaxPlayers} players.");
            }
        }
    }

    [HarmonyPatch(typeof(PlayFabMultiplayerManager), nameof(PlayFabMultiplayerManager.CreateAndJoinNetwork),
        typeof(PlayFabNetworkConfiguration))]
    internal static class IncreasePlayFabNetworkPlayerLimitPatch
    {
        private static void Prefix(PlayFabNetworkConfiguration networkConfiguration)
        {
            if (networkConfiguration != null && ExpandedServerPlugin.IsLocalServer &&
                networkConfiguration.MaxPlayerCount != ExpandedServerPlugin.PlayFabCapacity)
            {
                networkConfiguration.MaxPlayerCount = ExpandedServerPlugin.PlayFabCapacity;
                ExpandedServerPlugin.Log.LogDebug(
                    $"PlayFab network capacity set to {networkConfiguration.MaxPlayerCount} network members " +
                    $"for {ExpandedServerPlugin.MaxPlayers} players.");
            }
        }
    }
}
