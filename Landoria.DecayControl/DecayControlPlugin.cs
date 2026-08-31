using BepInEx;
using Landoria.SharedLib;

namespace Landoria.DecayControl
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class DecayControlPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.DecayControl";
        private const string PluginName = "Landoria.DecayControl";
        private const string PluginVersion = "1.0.1";

        internal static ModLog Log { get; private set; }
        internal static DecayControlSettings Settings { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            Settings = new DecayControlSettings();
            ShowDecayCommand.Register();
            Settings.InitializeServer(Log);
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void Update()
        {
            Settings.InitializeServer(Log);
            DecayStateRpc.Update();
        }

        private void OnDestroy()
        {
            DecayStateRpc.ResetSession();
            DecayProtection.Reset();
            DecayIndicators.Reset();
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Settings = null;
            Log = null;
        }
    }
}
