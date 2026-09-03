using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HarmonyLib;
using PlayFab;
using PlayFab.MultiplayerModels;

namespace Landoria.ExpandedServer
{
    internal static class PlayFabConnectionDiagnostics
    {
        private static readonly ConditionalWeakTable<ZPlayFabSocket, Attempt> Attempts =
            new ConditionalWeakTable<ZPlayFabSocket, Attempt>();
        private static int nextAttempt;

        internal static void Start(ZPlayFabSocket socket)
        {
            var attempt = new Attempt(Interlocked.Increment(ref nextAttempt));
            Attempts.Add(socket, attempt);
            ExpandedServerPlugin.Log?.LogInfo($"PlayFab connection attempt {attempt.Id} started.");
        }

        internal static void SessionFound(ZPlayFabSocket socket, PlayFabMatchmakingServerData server)
        {
            if (!Attempts.TryGetValue(socket, out Attempt attempt)) return;
            ExpandedServerPlugin.Log?.LogInfo(
                $"PlayFab connection attempt {attempt.Id} resolved lobby={Fingerprint(server?.lobbyId)}, " +
                $"network={Fingerprint(server?.networkId)}.");
        }

        internal static void NetworkJoined(ZPlayFabSocket socket, string networkId)
        {
            if (!Attempts.TryGetValue(socket, out Attempt attempt)) return;
            ExpandedServerPlugin.Log?.LogInfo(
                $"PlayFab connection attempt {attempt.Id} joined network={Fingerprint(networkId)}.");
        }

        internal static void Connected(ZPlayFabSocket socket)
        {
            if (!Attempts.TryGetValue(socket, out Attempt attempt) || attempt.Connected) return;
            attempt.Connected = true;
            ExpandedServerPlugin.Log?.LogInfo(
                $"PlayFab connection attempt {attempt.Id} established the remote transport.");
        }

        internal static void Failed(ZPlayFabSocket socket, ZPLayFabMatchmakingFailReason reason)
        {
            if (!Attempts.TryGetValue(socket, out Attempt attempt)) return;
            attempt.Failed = true;
            ExpandedServerPlugin.Log?.LogWarning(
                $"PlayFab connection attempt {attempt.Id} failed while locating the network: {reason}.");
        }

        internal static void Closed(ZPlayFabSocket socket)
        {
            if (!Attempts.TryGetValue(socket, out Attempt attempt)) return;
            string outcome = attempt.Failed ? "failed" : attempt.Connected ? "connected" : "incomplete";
            ExpandedServerPlugin.Log?.LogInfo(
                $"PlayFab connection attempt {attempt.Id} socket closed; outcome={outcome}.");
            Attempts.Remove(socket);
        }

        internal static void LobbyFailure(string stage, PlayFabError error, string lobbyId = null)
        {
            ExpandedServerPlugin.Log?.LogWarning(
                $"PlayFab lobby stage {stage} failed for lobby={Fingerprint(lobbyId)}: " +
                $"code={error?.Error}, http={error?.HttpCode}, message={error?.ErrorMessage ?? "unavailable"}.");
        }

        internal static string Fingerprint(string value)
        {
            if (string.IsNullOrEmpty(value)) return "none";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty);
            }
        }

        private sealed class Attempt
        {
            internal Attempt(int id) => Id = id;
            internal int Id { get; }
            internal bool Connected { get; set; }
            internal bool Failed { get; set; }
        }
    }

    [HarmonyPatch(typeof(ZPlayFabSocket), MethodType.Constructor,
        typeof(string), typeof(Action<PlayFabMatchmakingServerData>))]
    internal static class PlayFabClientSocketCreatedPatch
    {
        private static void Postfix(ZPlayFabSocket __instance) =>
            PlayFabConnectionDiagnostics.Start(__instance);
    }

    [HarmonyPatch(typeof(ZPlayFabSocket), "OnRemotePlayerSessionFound")]
    internal static class PlayFabSessionFoundPatch
    {
        private static void Prefix(ZPlayFabSocket __instance, PlayFabMatchmakingServerData serverData) =>
            PlayFabConnectionDiagnostics.SessionFound(__instance, serverData);
    }

    [HarmonyPatch(typeof(ZPlayFabSocket), "OnRemotePlayerNotFound")]
    internal static class PlayFabSessionNotFoundPatch
    {
        private static void Prefix(ZPlayFabSocket __instance, ZPLayFabMatchmakingFailReason failReason) =>
            PlayFabConnectionDiagnostics.Failed(__instance, failReason);
    }

    [HarmonyPatch(typeof(ZPlayFabSocket), "OnNetworkJoined")]
    internal static class PlayFabNetworkJoinedPatch
    {
        private static void Prefix(ZPlayFabSocket __instance, string networkId) =>
            PlayFabConnectionDiagnostics.NetworkJoined(__instance, networkId);
    }

    [HarmonyPatch(typeof(ZPlayFabSocket), "Connect")]
    internal static class PlayFabTransportConnectedPatch
    {
        private static void Postfix(ZPlayFabSocket __instance) =>
            PlayFabConnectionDiagnostics.Connected(__instance);
    }

    [HarmonyPatch(typeof(ZPlayFabSocket), "Dispose")]
    internal static class PlayFabSocketDisposedPatch
    {
        private static void Prefix(ZPlayFabSocket __instance) =>
            PlayFabConnectionDiagnostics.Closed(__instance);
    }

    [HarmonyPatch(typeof(ZPlayFabLobbySearch), "OnJoinLobbyFailed")]
    internal static class PlayFabJoinLobbyFailedPatch
    {
        private static void Prefix(PlayFabError error, string lobbyId) =>
            PlayFabConnectionDiagnostics.LobbyFailure("JoinLobby", error, lobbyId);
    }

    [HarmonyPatch(typeof(ZPlayFabLobbySearch), "OnGetLobbyFailed")]
    internal static class PlayFabGetLobbyFailedPatch
    {
        private static void Prefix(PlayFabError error) =>
            PlayFabConnectionDiagnostics.LobbyFailure("GetLobby", error);
    }
}
