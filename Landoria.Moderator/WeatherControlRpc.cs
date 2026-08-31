using System;

namespace Landoria.Moderator
{
    internal static class WeatherControlRpc
    {
        private const string SetWeatherRpc = "Landoria_Moderator_SetWeather";
        private const string ApplyWeatherRpc = "Landoria_Moderator_ApplyWeather";
        private static ZRoutedRpc _registeredRpc;

        internal static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || ReferenceEquals(rpc, _registeredRpc)) return;
            rpc.Register<string>(SetWeatherRpc, ReceiveRequest);
            rpc.Register<string>(ApplyWeatherRpc, ReceiveAppliedWeather);
            _registeredRpc = rpc;
        }

        internal static void Request(string environment)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(SetWeatherRpc, environment ?? "");
        }

        internal static void ResetSession() { _registeredRpc = null; }

        private static void ReceiveRequest(long sender, string environment)
        {
            if (!IsAuthorizedModerator(sender) || EnvMan.instance == null) return;
            if (!string.IsNullOrEmpty(environment) && !EnvironmentExists(environment))
            {
                ModeratorPlugin.ModLogger.LogWarning(
                    $"Unknown environment '{environment}' rejected for peer {sender}.");
                return;
            }
            EnvMan.instance.m_debugEnv = environment;
            ZRoutedRpc.instance.InvokeRoutedRPC(
                ZRoutedRpc.Everybody, ApplyWeatherRpc, environment);
            ModeratorPlugin.ModLogger.LogInfo(string.IsNullOrEmpty(environment)
                ? $"Moderator reset the weather for peer {sender}."
                : $"Moderator set weather to '{environment}' for peer {sender}.");
        }

        private static void ReceiveAppliedWeather(long sender, string environment)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() ||
                ZNet.instance.GetPeer(sender) == null || EnvMan.instance == null) return;
            EnvMan.instance.m_debugEnv = environment;
            ModeratorPlugin.ModLogger.LogInfo(string.IsNullOrEmpty(environment)
                ? "Server reset the local weather override."
                : $"Server applied local weather override '{environment}'.");
        }

        private static bool EnvironmentExists(string environment)
        {
            return EnvMan.instance.m_environments.Exists(candidate =>
                string.Equals(candidate.m_name, environment,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAuthorizedModerator(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return false;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            bool isAdmin = peer != null && ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            ZDO zdo = peer != null ? ZDOMan.instance?.GetZDO(peer.m_characterID) : null;
            bool active = zdo?.GetBool(ModeratorState.ModeratorZdoKey) == true;
            if (isAdmin && active) return true;
            ModeratorPlugin.ModLogger.LogWarning(
                $"Unauthorized weather request rejected for peer {sender}.");
            return false;
        }
    }
}
