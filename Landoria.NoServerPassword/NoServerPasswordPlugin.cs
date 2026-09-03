using BepInEx;
using Landoria.SharedLib;

namespace Landoria.NoServerPassword
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class NoServerPasswordPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.NoServerPassword";
        private const string PluginName = "Landoria.NoServerPassword";
        private const string PluginVersion = "1.0.7";

        internal static ModLog Log { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
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
