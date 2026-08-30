using System;

namespace Landoria.Moderator
{
    internal static class EventControlRpc
    {
        private const string StartRpc = "Landoria_Moderator_StartEvent";
        private const string StopRpc = "Landoria_Moderator_StopEvent";
        private static ZRoutedRpc _registeredRpc;

        internal static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || ReferenceEquals(rpc, _registeredRpc)) return;
            rpc.Register<string>(StartRpc, ReceiveStartRequest);
            rpc.Register(StopRpc, ReceiveStopRequest);
            _registeredRpc = rpc;
            ModeratorPlugin.ModLogger.LogDebug("Event control RPCs registered.");
        }

        internal static void RequestStart(string eventName)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(StartRpc, eventName);
        }

        internal static void RequestStop()
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(StopRpc);
        }

        internal static void ResetSession() { _registeredRpc = null; }

        private static void ReceiveStartRequest(long sender, string eventName)
        {
            ZNetPeer peer = GetAuthorizedModerator(sender);
            if (peer == null || RandEventSystem.instance == null) return;
            if (!RandEventSystem.instance.HaveEvent(eventName))
            {
                ModeratorPlugin.ModLogger.LogWarning($"Unknown event '{eventName}' rejected.");
                return;
            }
            RandEventSystem.instance.SetRandomEventByName(eventName, peer.m_refPos);
            ModeratorPlugin.ModLogger.LogInfo($"Moderator started event '{eventName}' for peer {sender}.");
        }

        private static void ReceiveStopRequest(long sender)
        {
            if (GetAuthorizedModerator(sender) == null || RandEventSystem.instance == null) return;
            RandEventSystem.instance.ResetRandomEvent();
            ModeratorPlugin.ModLogger.LogInfo($"Moderator stopped the active event for peer {sender}.");
        }

        private static ZNetPeer GetAuthorizedModerator(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return null;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            bool isAdmin = peer != null && ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            ZDO playerZdo = peer != null ? ZDOMan.instance?.GetZDO(peer.m_characterID) : null;
            bool moderatorActive = playerZdo?.GetBool(ModeratorState.ModeratorZdoKey) == true;
            if (isAdmin && moderatorActive) return peer;
            ModeratorPlugin.ModLogger.LogWarning($"Unauthorized event request rejected for peer {sender}.");
            return null;
        }
    }
}
