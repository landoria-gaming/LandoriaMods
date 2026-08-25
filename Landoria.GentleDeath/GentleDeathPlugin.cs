using BepInEx;
using Landoria.SharedLib;

namespace Landoria.GentleDeath
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GentleDeathPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.GentleDeath";
        private const string PluginName = "Landoria.GentleDeath";
        private const string PluginVersion = "1.0.10";

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
