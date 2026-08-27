using System;

namespace Landoria.CharacterVault
{
    public static class CharacterActivityApi
    {
        public static bool IsReady => CharacterActivityRegistry.IsReady;

        public static bool TryGetPlatformLastConnectedUtc(
            string platformPlayerId, out DateTime lastConnectedUtc)
        {
            return CharacterActivityRegistry.TryGetPlatformLastConnectedUtc(
                platformPlayerId, out lastConnectedUtc);
        }

        public static bool TryGetCharacterLastConnectedUtc(
            long characterId, out DateTime lastConnectedUtc)
        {
            return CharacterActivityRegistry.TryGetCharacterLastConnectedUtc(
                characterId, out lastConnectedUtc);
        }
    }
}
