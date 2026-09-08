using System;
using System.Linq;

namespace Landoria.RavenWatch
{
    internal static class ObserverEventGuard
    {
        internal static bool IsObservationOfOtherPlayer(object entry)
        {
            if (entry is PlayerAppearanceObserved player)
                return DifferentPlayers(player.observerCharacterId, player.characterId) &&
                    RemoteOwner(player.observerSessionId, player.ownerSessionId);
            if (entry is PlayerDebugFlyObserved flight)
                return DifferentPlayers(flight.observerCharacterId, flight.characterId) &&
                    RemoteOwner(flight.observerSessionId, flight.ownerSessionId);
            if (entry is CreatureAppearanceObserved creature)
                return RemoteOwner(creature.observerSessionId, creature.ownerSessionId) &&
                    HasOtherPlayer(creature.nearbyPlayers);
            if (entry is ItemObservation item)
                return item.groundEvidence != null &&
                    RemoteOwner(item.groundEvidence.observerSessionId,
                        item.groundEvidence.ownerSessionId) &&
                    HasOtherPlayer(item.groundEvidence.players);
            if (entry is ContainerObservation container)
                return DifferentPlayers(container.observerCharacterId,
                    container.openerCharacterId) &&
                    RemoteOwner(container.observerSessionId, container.ownerSessionId);
            return false;
        }

        private static bool HasOtherPlayer(NearbyPlayerEvidence[] players)
        {
            return players != null && players.Any(player => player != null && !player.isObserver);
        }

        private static bool DifferentPlayers(string observerId, string playerId)
        {
            return !string.IsNullOrWhiteSpace(observerId) &&
                !string.IsNullOrWhiteSpace(playerId) &&
                !string.Equals(observerId, playerId, StringComparison.Ordinal);
        }

        private static bool RemoteOwner(string observerSessionId, string ownerSessionId)
        {
            return !string.IsNullOrWhiteSpace(observerSessionId) &&
                !string.IsNullOrWhiteSpace(ownerSessionId) && ownerSessionId != "0" &&
                !string.Equals(observerSessionId, ownerSessionId, StringComparison.Ordinal);
        }
    }
}
