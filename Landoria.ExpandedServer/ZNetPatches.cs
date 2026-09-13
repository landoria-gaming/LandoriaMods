using System;
using HarmonyLib;

namespace Landoria.ExpandedServer
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.GetNrOfPlayers))]
    internal static class AllowPeerInfoUntilServerPlayerLimitPatch
    {
        private static bool _logged;

        private static void Postfix(ref int __result)
        {
            if (!ExpandedServerPlugin.IsLocalServer ||
                !IncreaseServerPlayerLimitPatch.IsCheckingPeerInfo)
            {
                return;
            }

            int playerCount = __result;
            if (playerCount >= ExpandedServerPlugin.MaxPlayers)
            {
                __result = ZNet.ServerPlayerLimit;
            }
            else if (playerCount >= ZNet.ServerPlayerLimit)
            {
                __result = ZNet.ServerPlayerLimit - 1;
                LogRaisedAdmissionLimit();
            }
        }

        private static void LogRaisedAdmissionLimit()
        {
            if (_logged)
            {
                return;
            }

            _logged = true;
            ExpandedServerPlugin.Log.LogDebug(
                $"Server admission limit increased to {ExpandedServerPlugin.MaxPlayers} players.");
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class IncreaseServerPlayerLimitPatch
    {
        [ThreadStatic]
        private static int _peerInfoDepth;

        internal static bool IsCheckingPeerInfo => _peerInfoDepth > 0;

        private static void Prefix()
        {
            _peerInfoDepth++;
        }

        private static void Finalizer()
        {
            if (_peerInfoDepth > 0)
            {
                _peerInfoDepth--;
            }
        }
    }
}
