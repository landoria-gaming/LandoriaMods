using System;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal sealed class ItemObservation
    {
        public int schemaVersion = 1;
        public string kind;
        public long sequence;
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observerName;
        public string observerCharacterId;
        public Vector3? observerPosition;
        public InventoryItemSnapshot item;
        public int quantity;
        public string observation;
        public string sourceNetworkId;
        public Vector3? sourcePosition;
        public GroundItemEvidence groundEvidence;

        internal static ItemObservation Capture(string kind, ItemDrop.ItemData item)
        {
            Player player = Player.m_localPlayer;
            return new ItemObservation
            {
                kind = kind, item = InventoryItemSnapshot.Capture(item), quantity = item.m_stack,
                observerName = player ? player.GetPlayerName() : null,
                observerCharacterId = player ? player.GetPlayerID().ToString() : null,
                observerPosition = player ? (Vector3?)player.transform.position : null
            };
        }
    }
}
