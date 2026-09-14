using BepInEx;
using Landoria.SharedLib;

namespace Landoria.FirstPerson
{
    // Starts and stops the First Person mod.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class FirstPersonPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.FirstPerson";
        private const string PluginName = "Landoria.FirstPerson";
        private const string PluginVersion = "1.0.9";

        internal const float HeadBobMultiplier = 2.0f; // Unitless: 0 disables, 1 normal, 2 doubled.

        internal const float HeadBobWalkVerticalAmplitude = 4.0f; // Millimeters.
        internal const float HeadBobWalkStepInterval = 0.7f; // Seconds per step.

        internal const float HeadBobJogVerticalAmplitude = 6.0f; // Millimeters.
        internal const float HeadBobJogStepInterval = 0.433f; // Seconds per step.

        internal const float HeadBobSprintVerticalAmplitude = 8.0f; // Millimeters.
        internal const float HeadBobSprintStepInterval = 0.333f; // Seconds per step.

        internal const float HeadBobHorizontalAmplitude = 4.0f; // Millimeters.

        internal const float HeadBobHorizonDistance = 10.0f; // Meters.

        internal static ModLog Log { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            FirstPersonPreference.Initialize(Config);
            FirstPersonCommand.Register();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void OnDestroy()
        {
            FirstPersonMode.Reset();
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Log = null;
        }
    }
}
