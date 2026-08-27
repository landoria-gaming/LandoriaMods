using Landoria.SharedLib;

namespace Landoria.StructureProtection
{
    internal sealed class StructureProtectionSettings
    {
        private bool serverInitialized;

        internal bool CreatureTargetingEnabled { get; private set; }
        internal bool WardPlayerDamageEnabled { get; private set; }
        internal int MaximumWardsPerCharacter { get; private set; } = -1;
        internal int WardInactivityDays { get; private set; } = -1;
        internal int WardInactivityCheckHours { get; private set; } = 6;

        internal void InitializeServer(ModLog logger)
        {
            if (serverInitialized || !ServerRole.IsDedicatedServer)
            {
                return;
            }
            StructureProtectionServerConfiguration configuration =
                StructureProtectionServerConfiguration.FromArguments(
                    System.Environment.GetCommandLineArgs());
            CreatureTargetingEnabled = configuration.CreatureTargetingEnabled;
            WardPlayerDamageEnabled = configuration.WardPlayerDamageEnabled;
            MaximumWardsPerCharacter = configuration.MaximumWardsPerCharacter;
            WardInactivityDays = configuration.WardInactivityDays;
            WardInactivityCheckHours = configuration.WardInactivityCheckHours;
            serverInitialized = true;
            logger.LogInfo($"Effective structure protection settings: " +
                $"creatureTargeting={CreatureTargetingEnabled}, " +
                $"wardPlayerDamage={WardPlayerDamageEnabled}, " +
                $"maximumWardsPerCharacter={MaximumWardsPerCharacter}, " +
                $"wardInactivityDays={WardInactivityDays}, " +
                $"wardInactivityCheckHours={WardInactivityCheckHours}.");
        }

        internal void WriteClientState(ZPackage package)
        {
            package.Write(CreatureTargetingEnabled);
            package.Write(MaximumWardsPerCharacter);
        }

        internal void ReadClientState(ZPackage package)
        {
            CreatureTargetingEnabled = package.ReadBool();
            MaximumWardsPerCharacter = package.ReadInt();
        }

        internal void ResetClientState()
        {
            if (!serverInitialized)
            {
                CreatureTargetingEnabled = false;
                WardPlayerDamageEnabled = false;
                MaximumWardsPerCharacter = -1;
            }
        }
    }
}
