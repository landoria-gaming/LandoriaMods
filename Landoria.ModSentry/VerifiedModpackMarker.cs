using System.Linq;

namespace Landoria.ModSentry
{
    public static class VerifiedModpackMarker
    {
        private const string Key = "landoria.modsentry_verified_modpack";
        private const string Value = "1";

        public static void Mark(ZRpc rpc)
        {
            ZNetPeer peer = FindPeer(rpc);
            if (peer != null)
            {
                peer.m_serverSyncedPlayerData[Key] = Value;
            }
        }

        public static bool IsMarked(string hostName)
        {
            return ZNet.instance?.GetPeers().Any(peer =>
                peer?.m_socket?.GetHostName() == hostName &&
                peer.m_serverSyncedPlayerData.TryGetValue(Key, out string value) &&
                value == Value) == true;
        }

        public static void Unmark(ZRpc rpc)
        {
            FindPeer(rpc)?.m_serverSyncedPlayerData.Remove(Key);
        }

        private static ZNetPeer FindPeer(ZRpc rpc)
        {
            return ZNet.instance?.GetPeers()
                .FirstOrDefault(peer => ReferenceEquals(peer.m_rpc, rpc));
        }
    }
}
