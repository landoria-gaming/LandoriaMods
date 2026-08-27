using System;

namespace Landoria.CharacterVault
{
    internal static class WorldSavePolicy
    {
        internal static void Handle(
            bool isServer, Action recordOnlineActivity, Action requestCharacterCheckpoint)
        {
            if (isServer)
            {
                recordOnlineActivity();
                requestCharacterCheckpoint();
            }
        }
    }
}
