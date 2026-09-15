using BepInEx;
using Landoria.SharedLib;

namespace Landoria.HuginnCam
{
    // Provides the entry point for the future autonomous cinematic camera.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class HuginnCamPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.HuginnCam";
        private const string PluginName = "Landoria.HuginnCam";
        private const string PluginVersion = "0.1.0";

        internal static ModLog Log { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            HuginnCamPreference.Initialize(Config);
            gameObject.AddComponent<ScreenRecorder>();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Log = null;
        }
    }
}
