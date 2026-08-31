using Landoria.SharedLib;

namespace Landoria.DecayControl
{
    internal sealed class DecayControlSettings
    {
        private bool serverInitialized;

        internal DecayControlSettings()
        {
            ResetState();
        }

        internal DecayControlMode FuelConsumption { get; private set; }
        internal DecayControlMode EnvironmentalBuildingWear { get; private set; }
        internal bool UsesPlayerActivity =>
            FuelConsumption == DecayControlMode.PlayerOnline ||
            EnvironmentalBuildingWear == DecayControlMode.PlayerOnline;

        internal void InitializeServer(ModLog logger)
        {
            if (serverInitialized || !ServerRole.IsDedicatedServer)
            {
                return;
            }
            DecayControlServerConfiguration configuration =
                DecayControlServerConfiguration.FromArguments(
                    System.Environment.GetCommandLineArgs());
            FuelConsumption = configuration.FuelConsumption;
            EnvironmentalBuildingWear = configuration.EnvironmentalBuildingWear;
            serverInitialized = true;
            logger.LogInfo($"Effective decay settings: fuelConsumption={FuelConsumption}, " +
                $"environmentalBuildingWear={EnvironmentalBuildingWear}.");
        }

        internal void WriteState(ZPackage package)
        {
            package.Write((int)FuelConsumption);
            package.Write((int)EnvironmentalBuildingWear);
        }

        internal void ReadState(ZPackage package)
        {
            FuelConsumption = (DecayControlMode)package.ReadInt();
            EnvironmentalBuildingWear = (DecayControlMode)package.ReadInt();
        }

        internal void ResetState()
        {
            if (serverInitialized)
            {
                return;
            }
            FuelConsumption = DecayControlMode.Default;
            EnvironmentalBuildingWear = DecayControlMode.Default;
        }
    }
}
