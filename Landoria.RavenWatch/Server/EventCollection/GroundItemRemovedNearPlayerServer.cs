using System;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class GroundItemRemovedNearPlayerServer
    {
        public int schemaVersion = 1;
        public string kind = "ground_item_removed_near_player_server";
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observation = "ground_item_destroyed_by_nearby_owner";
        public InventoryItemSnapshot item;
        public string itemNetworkId;
        public string playerName;
        public string playerSessionId;
        public Vector3 itemPosition;
        public Vector3 playerPosition;
        public float distanceFromPlayer;
        public string evidence = "item_owner_removed_ground_item_while_standing_near_it";
    }
}
