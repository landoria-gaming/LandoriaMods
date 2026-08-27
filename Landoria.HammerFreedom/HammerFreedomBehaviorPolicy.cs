namespace Landoria.HammerFreedom
{
    internal static class HammerFreedomBehaviorPolicy
    {
        internal static bool ShouldApplyDamage(
            bool isLocalPlayer, bool isFallDamage, bool fallImmunityAuthorized)
        {
            return !isLocalPlayer || !isFallDamage || !fallImmunityAuthorized;
        }

        internal static bool ShouldConsumeStamina(
            bool isLocalPlayer, bool unlimitedStaminaAuthorized)
        {
            return !isLocalPlayer || !unlimitedStaminaAuthorized;
        }

        internal static bool ShouldPreserveDurability(
            bool isLocalPlayer, bool noDurabilityLossAuthorized)
        {
            return isLocalPlayer && noDurabilityLossAuthorized;
        }

    }
}
