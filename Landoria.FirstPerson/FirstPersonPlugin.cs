using BepInEx;
using Landoria.SharedLib;

namespace Landoria.FirstPerson
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class FirstPersonPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.FirstPerson";
        private const string PluginName = "Landoria.FirstPerson";
        private const string PluginVersion = "1.0.7";

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
