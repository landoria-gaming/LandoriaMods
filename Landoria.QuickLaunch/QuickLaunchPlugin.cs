using BepInEx;
using Landoria.SharedLib;

namespace Landoria.QuickLaunch
{
    // Loads and unloads QuickLaunch.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class QuickLaunchPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.QuickLaunch";
        private const string PluginName = "Landoria.QuickLaunch";
        private const string PluginVersion = "1.0.10";

        private ModLog _log;

        // Loads the plugin.
        private void Awake()
        {
            _log = InitializePlugin(PluginGuid);
            QuickLaunchSession.Log = _log;
            RememberedPassword.Log = _log;
            _log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        // Unloads the plugin.
        private void OnDestroy()
        {
            _log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            QuickLaunchSession.Log = null;
            RememberedPassword.Log = null;
            _log = null;
        }
    }
}
