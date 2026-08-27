using System;

namespace Landoria.HammerFreedom
{
    [Flags]
    internal enum HammerFreedomCapabilities
    {
        None = 0,
        Flight = 1,
        FallDamageImmunity = 2,
        UnlimitedStamina = 4,
        NoDurabilityLoss = 8
    }

    internal static class HammerFreedomCapabilityPolicy
    {
        internal static HammerFreedomCapabilities Resolve(
            bool hammerWorld, bool flight, bool fallDamageImmunity, bool unlimitedStamina,
            bool noDurabilityLoss)
        {
            if (!hammerWorld)
            {
                return HammerFreedomCapabilities.None;
            }

            HammerFreedomCapabilities capabilities = HammerFreedomCapabilities.None;
            if (flight) capabilities |= HammerFreedomCapabilities.Flight;
            if (fallDamageImmunity) capabilities |= HammerFreedomCapabilities.FallDamageImmunity;
            if (unlimitedStamina) capabilities |= HammerFreedomCapabilities.UnlimitedStamina;
            if (noDurabilityLoss) capabilities |= HammerFreedomCapabilities.NoDurabilityLoss;
            return capabilities;
        }
    }
}
