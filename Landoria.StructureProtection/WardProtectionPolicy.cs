using System.Collections.Generic;

namespace Landoria.StructureProtection
{
    internal static class WardProtectionPolicy
    {
        internal static bool IsAuthorized(
            long creator, IEnumerable<long> permittedPlayers, long player)
        {
            if (player == 0L)
            {
                return false;
            }
            if (player == creator)
            {
                return true;
            }
            foreach (long permittedPlayer in permittedPlayers)
            {
                if (player == permittedPlayer)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
