using System;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    internal sealed class InventoryChangeScope
    {
        internal static int Suppressed;
        private static int depth;
        private Player player;
        private long characterId;
        private ZPackage before;
        private string operation;

        internal static InventoryChangeScope Begin(string operation)
        {
            var scope = new InventoryChangeScope();
            if (depth++ != 0) return scope;
            try
            {
                var local = Player.m_localPlayer;
                if (Suppressed != 0 || InventoryEntryDates.Suppressed != 0 || local == null
                    || ZNet.instance == null || ZNet.instance.IsServer()) return scope;
                scope.player = local;
                scope.characterId = local.GetPlayerID();
                scope.operation = operation;
                scope.before = InventorySnapshotCapture.Capture(local);
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
            return scope;
        }

        internal static Exception End(InventoryChangeScope scope, Exception error)
        {
            depth--;
            if (error != null) RavenWatchLog.Log.LogError(error);
            if (scope?.before == null || error != null) return error;
            try
            {
                if (scope.player == Player.m_localPlayer && scope.player.GetPlayerID() == scope.characterId)
                    InventoryChangedRpc.Send(scope.player, scope.operation, scope.before);
            }
            catch (Exception failure) { RavenWatchLog.Log.LogError(failure); }
            return error;
        }
    }
}
