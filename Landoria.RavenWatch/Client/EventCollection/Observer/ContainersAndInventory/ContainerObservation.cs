using System;
using System.Linq;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal sealed class ContainerObservation
    {
        public int schemaVersion = 1;
        public string kind = "container_open_observed";
        public long sequence;
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observerName;
        public string observerCharacterId;
        public string observerSessionId;
        public Vector3 observerPosition;
        public string containerName;
        public string containerNetworkId;
        public Vector3 containerPosition;
        public string previousOwnerSessionId;
        public string ownerSessionId;
        public string openerCharacterId = null;
        public string evidence = "replicated_in_use_transition";

        internal static ContainerObservation Capture(Container container, ZNetView view, long previousOwner)
        {
            Player player = Player.m_localPlayer;
            long owner = view.GetZDO().GetOwner();
            Player opener = Player.GetAllPlayers().FirstOrDefault(candidate =>
                IsOwnedByOtherPlayer(candidate, player, owner));
            return new ContainerObservation
            {
                observerName = player.GetPlayerName(),
                observerCharacterId = player.GetPlayerID().ToString(),
                observerSessionId = ZNet.instance ? ZNet.GetUID().ToString() : null,
                observerPosition = player.transform.position,
                containerName = container.gameObject.name,
                containerNetworkId = view.GetZDO().m_uid.ToString(),
                containerPosition = container.transform.position,
                previousOwnerSessionId = previousOwner.ToString(),
                ownerSessionId = owner.ToString(),
                openerCharacterId = opener ? opener.GetPlayerID().ToString() : null
            };
        }

        private static bool IsOwnedByOtherPlayer(Player candidate, Player observer, long owner)
        {
            if (!candidate || candidate == observer || owner == 0) return false;
            ZNetView view = candidate.GetComponent<ZNetView>();
            return view && view.IsValid() && view.GetZDO().GetOwner() == owner;
        }
    }
}
