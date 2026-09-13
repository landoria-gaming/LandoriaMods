using BepInEx;
using Landoria.SharedLib;

namespace Landoria.AfkDetector
{
    // Connects AFK detection to the BepInEx plugin lifecycle.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AfkDetectorPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.AfkDetector";
        private const string PluginName = "Landoria.AfkDetector";
        private const string PluginVersion = "1.0.11";
        internal static ModLog Log { get; private set; }

        // Starts the plugin.
        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            AfkDetectorServer.Start();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        // Advances server-side AFK detection.
        private void Update()
        {
            AfkDetectorServer.Tick();
        }

        // Stops the plugin and clears its state.
        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            AfkDetectorServer.Stop();
            ShutdownPlugin();
            Log = null;
        }
    }
}
