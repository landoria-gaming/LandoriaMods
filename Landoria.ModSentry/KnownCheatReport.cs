using System;
using System.Linq;

namespace Landoria.ModSentry
{
    internal static class KnownCheatReport
    {
        private const int ProtocolVersion = 1;
        private const int MaximumPackageBytes = 1024;
        private const int MaximumIndicatorLength = 160;

        internal static void Send(ZRpc serverRpc, string tool, string vector,
            string indicator)
        {
            ZPackage package = new ZPackage();
            package.Write(ProtocolVersion);
            package.Write(tool ?? string.Empty);
            package.Write(vector ?? string.Empty);
            package.Write(indicator ?? string.Empty);
            serverRpc.Invoke(ModSentryPlugin.CheatDetectionRpc, package);
            ModSentryPlugin.Log.LogWarning(
                $"Reported known managed cheat tool '{tool}' to the server.");
        }

        internal static void Receive(ZRpc rpc, ZPackage package)
        {
            ZNetPeer peer = FindPeer(rpc);
            if (peer == null)
            {
                return;
            }
            if (!TryRead(package, out string tool, out string vector,
                out string indicator, out string failure))
            {
                LogAndKick(peer, "invalid_cheat_report", "unknown", "unknown",
                    failure);
                return;
            }
            LogAndKick(peer, "known_cheat_tool", vector, indicator,
                $"tool='{tool}'");
        }

        private static bool TryRead(ZPackage package, out string tool,
            out string vector, out string indicator, out string failure)
        {
            tool = vector = indicator = null;
            failure = null;
            try
            {
                if (package == null || package.Size() > MaximumPackageBytes)
                {
                    failure = "package_size_invalid";
                    return false;
                }
                int protocol = package.ReadInt();
                tool = package.ReadString();
                vector = package.ReadString();
                indicator = package.ReadString();
                return Validate(protocol, tool, vector, indicator, out failure);
            }
            catch (Exception exception)
            {
                ModSentryPlugin.Log.LogWarning(
                    "Known-cheat report parsing failed: " + exception);
                failure = "parsing_failure_" + exception.GetType().Name;
                return false;
            }
        }

        private static bool Validate(int protocol, string tool, string vector,
            string indicator, out string failure)
        {
            failure = protocol != ProtocolVersion ? "protocol_invalid" :
                vector != "assembly_name" && vector != "type_namespace"
                    ? "vector_invalid" :
                string.IsNullOrWhiteSpace(indicator) ||
                    indicator.Length > MaximumIndicatorLength
                    ? "indicator_invalid" : null;
            if (failure == null &&
                !KnownCheatCatalog.Matches(tool, vector, indicator))
            {
                failure = "indicator_mismatch";
            }
            return failure == null;
        }

        private static void LogAndKick(ZNetPeer peer, string securityEvent,
            string vector, string indicator, string details)
        {
            string account = peer.m_socket?.GetHostName();
            ModSentryPlugin.Log.LogError(
                $"security_event={securityEvent} action=kick " +
                $"player='{Clean(peer.m_playerName)}' account='{Clean(account)}' " +
                $"endpoint='{Clean(peer.m_socket?.GetEndPointString())}' " +
                $"peer_uid={peer.m_uid} character_zdo={peer.m_characterID} " +
                $"vector='{Clean(vector)}' indicator='{Clean(indicator)}' " +
                details);
            if (string.IsNullOrWhiteSpace(account))
            {
                ZNet.instance?.Disconnect(peer);
                return;
            }
            ZNet.instance?.Kick(account);
        }

        private static string Clean(string value)
        {
            string clean = string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            return new string(clean.Where(character => character >= ' ' &&
                character != '\'' && character != '\r' && character != '\n')
                .Take(MaximumIndicatorLength).ToArray());
        }

        private static ZNetPeer FindPeer(ZRpc rpc)
        {
            return ZNet.instance?.GetPeers()
                .FirstOrDefault(peer => ReferenceEquals(peer.m_rpc, rpc));
        }
    }
}
