namespace Landoria.StructureProtection
{
    internal sealed class StructureProtectionServerConfiguration
    {
        internal bool CreatureTargetingEnabled { get; private set; }
        internal bool WardPlayerDamageEnabled { get; private set; }
        internal int MaximumWardsPerCharacter { get; private set; }
        internal int WardInactivityDays { get; private set; }
        internal int WardInactivityCheckHours { get; private set; }

        internal static StructureProtectionServerConfiguration FromArguments(string[] arguments)
        {
            return new StructureProtectionServerConfiguration
            {
                CreatureTargetingEnabled = StructureProtectionArgumentPolicy.Resolve(
                    arguments, "--structure-protection-offline-creature-targeting", true),
                WardPlayerDamageEnabled = StructureProtectionArgumentPolicy.Resolve(
                    arguments, "--structure-protection-ward-player-damage", true),
                MaximumWardsPerCharacter = StructureProtectionArgumentPolicy.ResolveMinimum(
                    arguments, "--structure-protection-max-wards-per-character", 5, -1),
                WardInactivityDays = StructureProtectionArgumentPolicy.ResolveMinimum(
                    arguments, "--structure-protection-ward-inactivity-days", 30, -1),
                WardInactivityCheckHours = StructureProtectionArgumentPolicy.ResolveMinimum(
                    arguments, "--structure-protection-ward-inactivity-check-hours", 6, 1)
            };
        }
    }
}
