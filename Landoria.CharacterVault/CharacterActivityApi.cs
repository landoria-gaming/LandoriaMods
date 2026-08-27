using System;

namespace Landoria.CharacterVault
{
    public static class CharacterActivityApi
    {
        public static bool IsReady => CharacterActivityRegistry.IsReady;

        public static bool TryGetPlatformLastSeenOnlineUtc(
            string platformPlayerId, out DateTime lastSeenOnlineUtc)
        {
            return CharacterActivityRegistry.TryGetPlatformLastSeenOnlineUtc(
                platformPlayerId, out lastSeenOnlineUtc);
        }

        public static bool TryGetCharacterLastSeenOnlineUtc(
            long characterId, out DateTime lastSeenOnlineUtc)
        {
            return CharacterActivityRegistry.TryGetCharacterLastSeenOnlineUtc(
                characterId, out lastSeenOnlineUtc);
        }
    }
}
