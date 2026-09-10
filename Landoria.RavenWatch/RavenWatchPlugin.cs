using Landoria.RavenWatch.Shared;
using BepInEx;
using Landoria.SharedLib;
using HarmonyLib;
using Landoria.RavenWatch.Server.Network;

namespace Landoria.RavenWatch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RavenWatchPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "Landoria.RavenWatch";
        private const string PluginName = "Landoria.RavenWatch";
        private const string PluginVersion = "1.0.0";

        private void Update()
        {
            if (ZNet.instance != null && ZNet.instance.IsDedicated()) InventoryPollServer.Tick();
        }

        private void Awake()
        {
            var log = new ModLog(Logger);
            RavenWatchLog.Log = log;
            RuntimePatches.Harmony = new Harmony(PluginGuid);
            RuntimePatches.Harmony.CreateClassProcessor(typeof(RuntimePatches)).Patch();
            log.LogInfo("RavenWatch proof of concept loaded.");
        }

    }
}
