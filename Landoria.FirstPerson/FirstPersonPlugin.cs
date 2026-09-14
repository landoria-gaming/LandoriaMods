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

        internal const bool HeadBobEnabled = true;
        // Head bob amplitudes are expressed in millimeters.
        internal const float HeadBobWalkVerticalAmplitude = 10.0f;
        internal const float HeadBobRunVerticalAmplitude = 20.0f;
        internal const float HeadBobHorizontalAmplitude = 0.0f;

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
