using BepInEx;
using Landoria.SharedLib;

namespace Landoria.StructureProtection
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class StructureProtectionPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.StructureProtection";
        private const string PluginName = "Landoria.StructureProtection";
        private const string PluginVersion = "1.0.5";

        internal static ModLog Log { get; private set; }
        internal static StructureProtectionSettings Settings { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            Settings = new StructureProtectionSettings();
            Settings.InitializeServer(Log);
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void Update()
        {
            Settings.InitializeServer(Log);
            CharacterActivityRegistry.Update();
            StructureProtectionSession.Update();
            WardQuota.Update();
            WardInactivityJob.Update();
        }

        private void OnDestroy()
        {
            CharacterActivityRegistry.Reset();
            StructureProtectionSession.Reset();
            WardQuota.Reset();
            WardInactivityJob.Reset();
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Settings = null;
            Log = null;
        }
    }
}
