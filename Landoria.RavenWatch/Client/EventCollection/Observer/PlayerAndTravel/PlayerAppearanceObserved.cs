using System;
using HarmonyLib;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal sealed class PlayerAppearanceObserved
    {
        public int schemaVersion = 1;
        public string kind = "player_appearance_observed";
        public long sequence;
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string worldId;
        public string observerName;
        public string observerCharacterId;
        public string observerSessionId;
        public Vector3? observerPosition;
        public string playerName;
        public string characterId;
        public string playerNetworkId;
        public string ownerSessionId;
        public Vector3 position;
        public Vector3 forward;
        public Vector3 velocity;
        public float? distance;
        public string evidence = "remote_player_instance_started";
        public string appearanceReason = "unknown";

        internal static PlayerAppearanceObserved Capture(Player player, ZNetView view)
        {
            Player observer = Player.m_localPlayer;
            return new PlayerAppearanceObserved
            {
                worldId = ZNet.instance ? ZNet.instance.GetWorldUID().ToString() : null,
                observerName = observer ? observer.GetPlayerName() : null,
                observerCharacterId = observer ? observer.GetPlayerID().ToString() : null,
                observerSessionId = ZNet.instance ? ZNet.GetUID().ToString() : null,
                observerPosition = observer ? (Vector3?)observer.transform.position : null,
                playerName = player.GetPlayerName(), characterId = player.GetPlayerID().ToString(),
                playerNetworkId = view.GetZDO().m_uid.ToString(),
                ownerSessionId = view.GetZDO().GetOwner().ToString(),
                position = player.transform.position, forward = player.transform.forward,
                velocity = player.GetVelocity(), distance = observer
                    ? (float?)Vector3.Distance(observer.transform.position, player.transform.position) : null
            };
        }
    }

    [HarmonyPatch(typeof(Player), "Start")]
    internal static class PlayerAppearanceObservedPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance || __instance == Player.m_localPlayer) return;
            try
            {
                ZNetView view = __instance.GetComponent<ZNetView>();
                if (!view || !view.IsValid() || view.IsOwner()) return;
                ActivityJournal.Record(PlayerAppearanceObserved.Capture(__instance, view));
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }
}
