using BepInEx;
using BepInEx.Configuration;
using Landoria.SharedLib;

namespace Landoria.HammerFreedom
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class HammerFreedomPlugin : LandoriaPlugin
    {
        internal const string PluginGuid = "Landoria.HammerFreedom";
        internal const string PluginName = "Landoria.HammerFreedom";
        internal const string PluginVersion = "1.0.6";
        private static readonly KeyboardShortcut ToggleShortcut =
            new KeyboardShortcut(UnityEngine.KeyCode.Z);

        internal static ModLog ModLogger { get; private set; }
        internal static HammerFreedomSettings Settings { get; private set; }
        private static bool settingsInitialized;

        private void Awake()
        {
            ModLogger = InitializePlugin(PluginGuid);
            Settings = new HammerFreedomSettings();
            FlyCommand.Register();
            ModLogger.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        internal static void InitializeDedicatedServerSettings()
        {
            if (settingsInitialized || !ServerRole.IsDedicatedServer) return;
            Settings = HammerFreedomSettings.FromArguments(
                System.Environment.GetCommandLineArgs(), ModLogger);
            settingsInitialized = true;
        }

        private void Update()
        {
            HammerFreedomAuthorization.Update();
            HandleShortcuts();
        }

        private void HandleShortcuts()
        {
            if (!FlyInput.IsAvailable())
            {
                return;
            }

            if (ToggleShortcut.IsDown())
            {
                FlyController.Toggle();
            }
        }

        private void OnDestroy()
        {
            HammerFreedomAuthorization.ResetSession();
            ModLogger?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Settings = null;
            settingsInitialized = false;
            ModLogger = null;
        }
    }
}
