using System.Collections.Generic;

namespace Landoria.Socialize
{
    internal static class MapSharingPolicy
    {
        internal static bool CanSendPublicPing(bool restricted, bool isInGroup) =>
            !restricted || isInGroup;

        internal static bool GetPublicPosition(bool restricted, bool requested) =>
            restricted ? false : requested;

        internal static bool ShouldAddGroupMember(
            long playerId, long localPlayerId, ISet<long> visiblePlayerIds)
        {
            return playerId != localPlayerId && !visiblePlayerIds.Contains(playerId);
        }

    }
}
