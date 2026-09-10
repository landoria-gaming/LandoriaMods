using System.Runtime.CompilerServices;

namespace Landoria.ServerInventory.Server
{
    internal static class InventoryChangesServer
    {
        private sealed class Session { internal bool Loaded; }
        private static readonly ConditionalWeakTable<ZRpc, Session> sessions = new ConditionalWeakTable<ZRpc, Session>();
        internal static void Register(ZNetPeer peer) => sessions.GetOrCreateValue(peer.m_rpc);
        internal static bool IsLoaded(ZRpc rpc) => InventoryCommitLog.Healthy && sessions.TryGetValue(rpc, out var session) && session.Loaded;
        internal static void MarkLoaded(ZRpc rpc) => sessions.GetOrCreateValue(rpc).Loaded = true;
    }
}
