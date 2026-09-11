namespace Landoria.CharacterVault
{
    internal static class NewCharacterPolicy
    {
        internal static bool HasNeverJoinedAWorld(PlayerProfile profile)
        {
            return profile.m_firstSpawn && profile.m_playerStats[0].m_knownWorlds.Count == 0;
        }
    }
}
