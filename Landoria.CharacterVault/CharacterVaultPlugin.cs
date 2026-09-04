using System.Collections;
using System.Threading;
using BepInEx;
using Landoria.SharedLib;
using UnityEngine;

namespace Landoria.CharacterVault
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("Landoria.ModSentry", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class CharacterVaultPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.CharacterVault";
        private const string PluginName = "Landoria.CharacterVault";
        private const string PluginVersion = "1.0.26";
        internal static ModLog Log { get; private set; }
        internal static GracefulShutdownCoordinator Coordinator { get; private set; }
        internal static VoluntaryDisconnectCoordinator DisconnectCoordinator { get; private set; }
        internal static ServerDisconnectSaveCoordinator ServerDisconnects { get; private set; }
        internal static CharacterSaveStatusDisplay SaveStatus { get; private set; }
        internal static CharacterVaultPlugin Instance { get; private set; }
        internal static CharacterVaultSettings Settings { get; private set; }
        internal static ProfileTransferService Transfers { get; private set; }
        internal static WindowsCloseInterceptor WindowsClose { get; private set; }
        internal static bool PlayFabVerboseLogging { get; private set; }

        private void Awake()
        {
            Instance = this;
            PlayFabVerboseLogging = Config.Bind(
                "Diagnostics",
                "PlayFabVerboseLogging",
                false,
                "Enables verbose PlayFab Party logging for local diagnostics.").Value;
            Log = InitializePlugin(PluginGuid);
            Settings = new CharacterVaultSettings();
            Transfers = new ProfileTransferService(SynchronizationContext.Current);
            Coordinator = new GracefulShutdownCoordinator(SynchronizationContext.Current);
            DisconnectCoordinator = new VoluntaryDisconnectCoordinator();
            WindowsClose = new WindowsCloseInterceptor();
            ServerDisconnects = new ServerDisconnectSaveCoordinator();
            SaveStatus = new CharacterSaveStatusDisplay();
            PlayFabVerboseDiagnostics.Enable();
            CharacterVaultLobbyLeftDiagnostics.Register();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        internal void Run(IEnumerator routine)
        {
            StartCoroutine(routine);
        }

        internal void QuitNextFrame()
        {
            StartCoroutine(QuitAfterCurrentFrame());
        }

        private void Update()
        {
            CharacterVaultRejection.Tick();
            CharacterActivityRegistry.Update();
            Transfers.RecordReadyActivity();
            Transfers.MonitorFinalSaves();
            MonitorWindowsClose();
        }

        private static void MonitorWindowsClose()
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated())
            {
                return;
            }

            WindowsClose.EnsureInstalled();
            if (WindowsClose.ConsumeCloseRequest())
            {
                DisconnectCoordinator.HandleNativeCloseRequest();
            }
        }

        private static IEnumerator QuitAfterCurrentFrame()
        {
            yield return null;
            Application.Quit();
        }

        private void OnDestroy()
        {
            CharacterVaultLobbyLeftDiagnostics.Unregister();
            DisconnectCoordinator?.Dispose();
            WindowsClose?.Dispose();
            ServerDisconnects?.Dispose();
            Coordinator?.Dispose();
            Transfers?.Dispose();
            SaveStatus?.Dispose();
            CharacterActivityRegistry.Reset();
            CharacterVaultRejection.Clear();
            DisconnectCoordinator = null;
            WindowsClose = null;
            ServerDisconnects = null;
            Coordinator = null;
            Transfers = null;
            SaveStatus = null;
            Settings = null;
            PlayFabVerboseLogging = false;
            Instance = null;
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Log = null;
        }
    }
}
