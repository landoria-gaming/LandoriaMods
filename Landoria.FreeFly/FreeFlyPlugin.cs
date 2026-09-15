using BepInEx;
using Landoria.SharedLib;

namespace Landoria.FreeFly
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Loads and unloads the FreeFly mod.
    public sealed class FreeFlyPlugin : LandoriaPlugin
    {
        internal const string PluginGuid = "Landoria.FreeFly";
        internal const string PluginName = "Landoria.FreeFly";
        internal const string PluginVersion = "1.0.0";
        private static ModLog ModLogger { get; set; }

        // Loads settings, commands, and patches.
        private void Awake()
        {
            ModLogger = InitializePlugin(PluginGuid);
            FreeFlyPreference.Initialize(Config);
            FreeFlyCommands.Register();
            ModLogger.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        // Reads shortcuts and updates interface visibility.
        private void Update()
        {
            FreeFlyShortcut.Update();
            FreeFlyInterfaceController.Update();
        }

        // Restores game state when the mod unloads.
        private void OnDestroy()
        {
            FreeFlyController.CompleteDisable();
            FreeFlyController.Reset();
            FreeFlyInterfaceController.Restore();
            ModLogger?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            ModLogger = null;
        }
    }
}
