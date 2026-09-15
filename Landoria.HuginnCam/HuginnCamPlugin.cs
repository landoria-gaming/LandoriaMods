using BepInEx;
using Landoria.SharedLib;
using Landoria.UnityMediaRecorder;

namespace Landoria.HuginnCam
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Provides the entry point for the autonomous cinematic camera recorder.
    public sealed class HuginnCamPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.HuginnCam";
        private const string PluginName = "Landoria.HuginnCam";
        private const string PluginVersion = "1.0.0";

        internal static ModLog Log { get; private set; }

        // Initializes the plugin and attaches the recording controller.
        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            MediaRecorderLog.Info = Log.LogInfo;
            MediaRecorderLog.Warning = Log.LogWarning;
            MediaRecorderLog.Error = Log.LogError;
            HuginnCamPreference.Initialize(Config);
            gameObject.AddComponent<ScreenRecorder>();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        // Releases plugin resources when BepInEx unloads the plugin.
        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            MediaRecorderLog.Info = null;
            MediaRecorderLog.Warning = null;
            MediaRecorderLog.Error = null;
            Log = null;
        }
    }
}
