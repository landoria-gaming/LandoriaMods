using System;
using UnityEngine;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class GroundItemSpawnedServer
    {
        public int schemaVersion = 1;
        public string kind = "ground_item_spawned_server";
        public string utc = DateTime.UtcNow.ToString("O");
        public float elapsedSeconds = Time.realtimeSinceStartup;
        public string observation = "new_ground_item_received_by_server";
        public InventoryItemSnapshot item;
        public string itemNetworkId;
        public string itemOwnerSessionId;
        public string networkSenderSessionId;
        public string networkSenderName;
        public string characterHistoryId;
        public string characterFirstSeenUtc;
        public float? characterFirstSeenSecondsAgo;
        public PlayerEquipmentSnapshot playerEquipment;
        public bool characterAppearsNew;
        public bool networkSenderWasOwner;
        public bool networkIdMatchesSender;
        public bool? pickedUp;
        public Vector3 position;
        public Vector3? velocity;
        public Vector3? playerPosition;
        public Vector3? playerForward;
        public float? distanceFromPlayer;
        public float? forwardAlignment;

        internal static GroundItemSpawnedServer Capture(ZDO zdo, GameObject prefab,
            ZNetPeer sender)
        {
            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (!drop || sender == null) return null;
            ZDO player = ZDOMan.instance?.GetZDO(sender.m_characterID);
            Vector3? playerPosition = player?.GetPosition();
            Vector3? playerForward = player != null
                ? (Vector3?)(player.GetRotation() * Vector3.forward) : null;
            Vector3 position = zdo.GetPosition();
            return Create(zdo, sender, drop.m_itemData, prefab.name, player, position,
                playerPosition, playerForward);
        }

        private static GroundItemSpawnedServer Create(ZDO zdo, ZNetPeer sender,
            ItemDrop.ItemData itemTemplate, string prefabName, ZDO player, Vector3 position,
            Vector3? playerPosition, Vector3? playerForward)
        {
            bool hasPickedUp = zdo.GetBool(ZDOVars.s_pickedUp, out bool pickedUp);
            bool hasVelocity = zdo.GetVec3(ZDOVars.s_velHash, out Vector3 velocity);
            Vector3? offset = playerPosition.HasValue ? position - playerPosition.Value : null;
            float? characterAge = ServerCharacterHistory.Observe(sender);
            PlayerEquipmentSnapshot equipment = PlayerEquipmentSnapshot.Capture(player);
            return new GroundItemSpawnedServer
            {
                item = InventoryItemSnapshot.Capture(itemTemplate, zdo, prefabName),
                itemNetworkId = zdo.m_uid.ToString(),
                itemOwnerSessionId = zdo.GetOwner().ToString(),
                networkSenderSessionId = sender.m_uid.ToString(),
                networkSenderName = sender.m_playerName,
                characterHistoryId = ServerCharacterHistory.GetIdentity(sender),
                characterFirstSeenUtc = ServerCharacterHistory.FirstSeenUtc(sender),
                characterFirstSeenSecondsAgo = characterAge,
                playerEquipment = equipment,
                characterAppearsNew = characterAge <= 300f && equipment?.starterOnly == true,
                networkSenderWasOwner = zdo.GetOwner() == sender.m_uid,
                networkIdMatchesSender = zdo.m_uid.UserID == sender.m_uid,
                pickedUp = hasPickedUp ? (bool?)pickedUp : null,
                position = position,
                velocity = hasVelocity ? (Vector3?)velocity : null,
                playerPosition = playerPosition,
                playerForward = playerForward,
                distanceFromPlayer = offset?.magnitude,
                forwardAlignment = Alignment(offset, playerForward)
            };
        }

        private static float? Alignment(Vector3? offset, Vector3? forward)
        {
            if (!offset.HasValue || !forward.HasValue || offset.Value.sqrMagnitude == 0f)
                return null;
            return Vector3.Dot(offset.Value.normalized, forward.Value.normalized);
        }
    }

    internal static class ServerGroundItemCollector
    {
        internal static void Observe(ZDO zdo, GameObject prefab, ZNetPeer sender)
        {
            try
            {
                GroundItemSpawnedServer entry = GroundItemSpawnedServer.Capture(zdo, prefab, sender);
                if (entry != null) ServerEventPublisher.Publish(entry);
            }
            catch (Exception exception) { RavenWatchPlugin.Log?.LogError(exception); }
        }
    }
}
