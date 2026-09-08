using System;
using System.Collections.Generic;
using System.Linq;
using Landoria.RavenWatch.Server.EventCollection;
using UnityEngine;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class GroundItemSpawnedDetection : ICheatDetection
    {
        private const string DetectionCode = "GROUND_ITEM_SPAWNED_OUTSIDE_INVENTORY";
        private const int SpawnAnomalyConfidence = 2;
        private const int SpawnAttributionConfidence = 2;
        private const int CollectedAnomalyConfidence = 2;
        private const int CollectedAttributionConfidence = 2;
        private const float SpawnBurstSeconds = 2f;

        public CheatDetectionFinding[] ServerBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            Dictionary<string, Event> pickups = IndexPickups(events);
            foreach (Event eventToAnalyze in events)
            {
                var item = eventToAnalyze.entry as GroundItemSpawnedServer;
                if (!IsSuspiciousServerItem(eventToAnalyze, item)) continue;
                Event pickup = FindPickup(pickups, item);
                if (!IsEquipment(item.item) && (pickup == null ||
                    CollectedBurstQuantity(events, pickups, item) < item.item.maximumStack)) continue;
                findings.Add(CreateServerFinding(eventToAnalyze, pickup, item));
            }
            return findings.ToArray();
        }

        public CheatDetectionFinding[] ObserverBasedDetection(IReadOnlyList<Event> events)
        {
            var findings = new List<CheatDetectionFinding>();
            foreach (Event eventToAnalyze in events)
            {
                var item = eventToAnalyze.entry as ItemObservation;
                NearbyPlayerEvidence suspect = FindObserverSuspect(eventToAnalyze, item);
                if (suspect == null) continue;
                findings.Add(CreateObserverFinding(eventToAnalyze, item, suspect));
            }
            return findings.ToArray();
        }

        private static bool IsSuspiciousServerItem(Event source, GroundItemSpawnedServer item)
        {
            return item != null && (string)source.context["source"] == "server" &&
                item.observation == "new_ground_item_received_by_server" &&
                item.pickedUp == false && item.item != null &&
                item.networkSenderWasOwner && item.networkIdMatchesSender &&
                IsInFront(item.distanceFromPlayer, item.forwardAlignment) &&
                !HasVanillaThrowVelocity(item.velocity, item.playerForward);
        }

        private static Dictionary<string, Event> IndexPickups(IReadOnlyList<Event> events)
        {
            var pickups = new Dictionary<string, Event>(StringComparer.Ordinal);
            foreach (Event candidate in events)
            {
                var pickup = candidate.entry as GroundItemRemovedNearPlayerServer;
                if (pickup == null || (string)candidate.context["source"] != "server" ||
                    string.IsNullOrWhiteSpace(pickup.itemNetworkId)) continue;
                if (!pickups.ContainsKey(pickup.itemNetworkId))
                    pickups.Add(pickup.itemNetworkId, candidate);
            }
            return pickups;
        }

        private static Event FindPickup(IReadOnlyDictionary<string, Event> pickups,
            GroundItemSpawnedServer item)
        {
            if (!pickups.TryGetValue(item.itemNetworkId, out Event candidate)) return null;
            var pickup = candidate.entry as GroundItemRemovedNearPlayerServer;
            return pickup != null && pickup.playerSessionId == item.networkSenderSessionId &&
                pickup.elapsedSeconds >= item.elapsedSeconds ? candidate : null;
        }

        private static int CollectedBurstQuantity(IReadOnlyList<Event> events,
            IReadOnlyDictionary<string, Event> pickups, GroundItemSpawnedServer reference)
        {
            int quantity = 0;
            foreach (Event candidate in events)
            {
                var item = candidate.entry as GroundItemSpawnedServer;
                if (!IsSuspiciousServerItem(candidate, item) ||
                    item.networkSenderSessionId != reference.networkSenderSessionId ||
                    item.item.prefab != reference.item.prefab ||
                    Math.Abs(item.elapsedSeconds - reference.elapsedSeconds) > SpawnBurstSeconds ||
                    FindPickup(pickups, item) == null) continue;
                quantity += item.item.quantity;
            }
            return quantity;
        }

        private static NearbyPlayerEvidence FindObserverSuspect(Event source, ItemObservation item)
        {
            if (item == null || (string)source.context["source"] != "observer" ||
                item.kind != "ground_item_observed" || item.item?.pickedUp != false ||
                !IsEquipment(item.item) || !item.sourcePosition.HasValue) return null;
            NearbyPlayerEvidence suspect = item.groundEvidence?.players?.FirstOrDefault(player =>
                player != null && !player.isObserver && player.matchesItemOwner);
            if (suspect == null) return null;
            Vector3 offset = item.sourcePosition.Value - suspect.position;
            float alignment = offset.sqrMagnitude == 0f ? -1f :
                Vector3.Dot(offset.normalized, suspect.forward.normalized);
            return IsInFront(offset.magnitude, alignment) &&
                !HasVanillaThrowVelocity(item.groundEvidence.velocity, suspect.forward)
                ? suspect : null;
        }

        private static bool IsEquipment(InventoryItemSnapshot item)
        {
            if (item == null) return false;
            return item.type == "OneHandedWeapon" || item.type == "Bow" ||
                item.type == "Shield" || item.type == "Helmet" || item.type == "Chest" ||
                item.type == "Legs" || item.type == "Hands" ||
                item.type == "TwoHandedWeapon" || item.type == "Torch" ||
                item.type == "Shoulder" || item.type == "Utility" || item.type == "Tool" ||
                item.type == "Attach_Atgeir" || item.type == "TwoHandedWeaponLeft" ||
                item.type == "Trinket";
        }

        private static bool IsInFront(float? distance, float? alignment)
        {
            return distance >= 0.5f && distance <= 4f && alignment >= 0.65f;
        }

        private static bool HasVanillaThrowVelocity(Vector3? velocity, Vector3? forward)
        {
            if (!velocity.HasValue || !forward.HasValue || velocity.Value.magnitude < 3f)
                return false;
            Vector3 expected = (forward.Value.normalized + Vector3.up).normalized;
            return Vector3.Dot(velocity.Value.normalized, expected) >= 0.5f;
        }

        private static CheatDetectionFinding CreateServerFinding(Event spawn, Event pickup,
            GroundItemSpawnedServer item)
        {
            bool collected = pickup != null;
            return new CheatDetectionFinding(collected ? new[] { spawn, pickup } : new[] { spawn },
                GetDetectionId(item.networkSenderSessionId, item.item.prefab), DetectionCode,
                collected ? CollectedAnomalyConfidence : SpawnAnomalyConfidence,
                collected ? CollectedAttributionConfidence : SpawnAttributionConfidence,
                collected ? "server_saw_direct_item_spawn_then_collection" :
                    "server_saw_direct_equipment_prefab_spawn", item.networkSenderName,
                item.networkSenderSessionId, "spawned a " + item.item.prefab,
                Explain(item, collected));
        }

        private static CheatDetectionFinding CreateObserverFinding(Event source,
            ItemObservation item, NearbyPlayerEvidence suspect)
        {
            return new CheatDetectionFinding(new[] { source },
                GetDetectionId(suspect.ownerSessionId, item.item.prefab), DetectionCode,
                SpawnAnomalyConfidence, SpawnAttributionConfidence,
                "observer_saw_direct_equipment_prefab_spawn", suspect.name,
                suspect.ownerSessionId, "spawned a " + item.item.prefab,
                Explain(item.item.prefab, false, null));
        }

        private static string Explain(GroundItemSpawnedServer item, bool collected)
        {
            string characterContext = item.characterAppearsNew
                ? " The server first recorded this character " +
                    Math.Round(item.characterFirstSeenSecondsAgo.Value) +
                    " seconds earlier, and the character had no visible equipment beyond starter rags or a torch. Together these details are consistent with a new character, but do not prove the profile creation time."
                : string.Empty;
            return Explain(item.item.prefab, collected, characterContext);
        }

        private static string Explain(string prefab, bool collected, string characterContext)
        {
            string collection = collected
                ? " The same player then removed it while standing next to it, which is consistent with collecting it. Resource findings require a collected burst at least as large as one full stack."
                : string.Empty;
            return "The " + prefab + " appeared directly in front of the player with " +
                "pickedUp=false and without vanilla item-throw motion. An item dropped from " +
                "a player inventory normally has pickedUp=true and receives forward velocity." +
                collection + characterContext;
        }

        private static string GetDetectionId(string playerSessionId, string prefab)
        {
            return string.IsNullOrWhiteSpace(playerSessionId) || string.IsNullOrWhiteSpace(prefab)
                ? null : DetectionCode + ":" + playerSessionId + ":" + prefab;
        }
    }
}
