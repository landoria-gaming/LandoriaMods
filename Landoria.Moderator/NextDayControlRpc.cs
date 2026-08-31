using System;

namespace Landoria.Moderator
{
    internal static class NextDayControlRpc
    {
        private const string NextDayRpc = "Landoria_Moderator_NextDay";
        private static ZRoutedRpc _registeredRpc;

        internal static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || ReferenceEquals(rpc, _registeredRpc)) return;
            rpc.Register(NextDayRpc, ReceiveRequest);
            _registeredRpc = rpc;
            ModeratorPlugin.ModLogger.LogDebug("Next-day control RPC registered.");
        }

        internal static void Request()
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(NextDayRpc);
        }

        internal static void ResetSession()
        {
            _registeredRpc = null;
        }

        private static void ReceiveRequest(long sender)
        {
            if (GetAuthorizedModerator(sender) == null || EnvMan.instance == null)
            {
                return;
            }
            EnvMan.instance.SkipToMorning();
            ModeratorPlugin.ModLogger.LogInfo(
                $"Moderator skipped to the next morning for peer {sender}.");
        }

        private static ZNetPeer GetAuthorizedModerator(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return null;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            bool isAdmin = peer != null &&
                ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            ZDO playerZdo = peer != null
                ? ZDOMan.instance?.GetZDO(peer.m_characterID)
                : null;
            bool moderatorActive =
                playerZdo?.GetBool(ModeratorState.ModeratorZdoKey) == true;
            if (isAdmin && moderatorActive) return peer;
            ModeratorPlugin.ModLogger.LogWarning(
                $"Unauthorized next-day request rejected for peer {sender}.");
            return null;
        }
    }
}
