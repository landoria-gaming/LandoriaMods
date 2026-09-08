using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal sealed class PlayerDebugFlyObserved
    {
        public int schemaVersion = 1;
        public string kind = "player_debug_fly_observed";
        public long sequence;
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string worldId;
        public string observerName;
        public string observerCharacterId;
        public string observerSessionId;
        public string playerName;
        public string characterId;
        public string playerNetworkId;
        public string ownerSessionId;
        public Vector3 position;
        public Vector3 velocity;
        public float? distance;
        public bool debugFly = true;
        public string evidence = "remote_player_debug_fly_flag_enabled";

        internal static PlayerDebugFlyObserved Capture(Player player, ZNetView view)
        {
            Player observer = Player.m_localPlayer;
            return new PlayerDebugFlyObserved
            {
                worldId = ZNet.instance ? ZNet.instance.GetWorldUID().ToString() : null,
                observerName = observer ? observer.GetPlayerName() : null,
                observerCharacterId = observer ? observer.GetPlayerID().ToString() : null,
                observerSessionId = ZNet.instance ? ZNet.GetUID().ToString() : null,
                playerName = player.GetPlayerName(),
                characterId = player.GetPlayerID().ToString(),
                playerNetworkId = view.GetZDO().m_uid.ToString(),
                ownerSessionId = view.GetZDO().GetOwner().ToString(),
                position = player.transform.position,
                velocity = player.GetVelocity(),
                distance = observer
                    ? (float?)Vector3.Distance(observer.transform.position, player.transform.position)
                    : null
            };
        }
    }

    internal static class PlayerDebugFlyObserver
    {
        private const float CheckIntervalSeconds = 0.5f;
        private sealed class State { internal bool debugFly; }
        private static ConditionalWeakTable<Player, State> states = new();
        private static float nextCheck;

        internal static void Open()
        {
            states = new ConditionalWeakTable<Player, State>();
            nextCheck = 0f;
        }

        internal static void Tick()
        {
            if (Time.realtimeSinceStartup < nextCheck) return;
            nextCheck = Time.realtimeSinceStartup + CheckIntervalSeconds;
            foreach (Player player in Player.GetAllPlayers()) Observe(player);
        }

        internal static void Observe(Player player)
        {
            if (!player || player == Player.m_localPlayer) return;
            try
            {
                ZNetView view = player.GetComponent<ZNetView>();
                if (!view || !view.IsValid() || view.IsOwner()) return;
                State state = states.GetValue(player, key => new State());
                bool debugFly = player.IsDebugFlying();
                if (debugFly && !state.debugFly)
                    ActivityJournal.Record(PlayerDebugFlyObserved.Capture(player, view));
                state.debugFly = debugFly;
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }
}
