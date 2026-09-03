using BepInEx;
using Landoria.SharedLib;

namespace Landoria.StructureProtection
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("Landoria.CharacterVault", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class StructureProtectionPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.StructureProtection";
        private const string PluginName = "Landoria.StructureProtection";
        private const string PluginVersion = "1.0.4";

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
            StructureProtectionSession.Update();
            WardQuota.Update();
            WardInactivityJob.Update();
        }

        private void OnDestroy()
        {
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
