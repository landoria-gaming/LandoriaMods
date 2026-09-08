using System.Linq;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal sealed class GroundItemEvidence
    {
        public string worldId;
        public string observerSessionId;
        public string ownerSessionIdAtInitialization;
        public string ownerSessionId;
        public string networkIdUserComponent;
        public string networkSpawnTicks;
        public Vector3? velocity;
        public float nearbyRadius = 30f;
        public NearbyPlayerEvidence[] players;

        internal static GroundItemEvidence Capture(ItemDrop item, ZNetView view, long? initialOwner)
        {
            ZDO zdo = view.GetZDO();
            Rigidbody body = item.GetComponent<Rigidbody>();
            return new GroundItemEvidence
            {
                worldId = ZNet.instance ? ZNet.instance.GetWorldUID().ToString() : null,
                observerSessionId = ZNet.instance ? ZNet.GetUID().ToString() : null,
                ownerSessionIdAtInitialization = initialOwner?.ToString(),
                ownerSessionId = zdo.GetOwner().ToString(),
                networkIdUserComponent = zdo.m_uid.UserID.ToString(),
                networkSpawnTicks = zdo.GetLong(ZDOVars.s_spawnTime, 0L).ToString(),
                velocity = body ? (Vector3?)body.linearVelocity : null,
                players = Player.GetAllPlayers().Where(player => player)
                    .Select(player => NearbyPlayerEvidence.Capture(player, item.transform.position, zdo.GetOwner()))
                    .Where(player => player.distance <= 30f || player.matchesItemOwner)
                    .OrderBy(player => player.distance).ToArray()
            };
        }
    }

    internal sealed class NearbyPlayerEvidence
    {
        public string name;
        public string characterId;
        public string networkId;
        public string ownerSessionId;
        public Vector3 position;
        public Vector3 forward;
        public Vector3 velocity;
        public float distance;
        public bool matchesItemOwner;
        public bool isObserver;

        internal static NearbyPlayerEvidence Capture(Player player, Vector3 itemPosition, long owner)
        {
            ZNetView view = player.GetComponent<ZNetView>();
            long? playerOwner = view && view.IsValid() ? (long?)view.GetZDO().GetOwner() : null;
            return new NearbyPlayerEvidence
            {
                name = player.GetPlayerName(), characterId = player.GetPlayerID().ToString(),
                networkId = player.GetZDOID().ToString(), ownerSessionId = playerOwner?.ToString(),
                position = player.transform.position, forward = player.transform.forward,
                velocity = player.GetVelocity(), distance = Vector3.Distance(player.transform.position, itemPosition),
                matchesItemOwner = owner != 0 && playerOwner == owner,
                isObserver = player == Player.m_localPlayer
            };
        }
    }
}
