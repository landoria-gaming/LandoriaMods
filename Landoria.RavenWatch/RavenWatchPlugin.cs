using BepInEx;
using Landoria.SharedLib;

namespace Landoria.RavenWatch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RavenWatchPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "Landoria.RavenWatch";
        private const string PluginName = "Landoria.RavenWatch";
        private const string PluginVersion = "1.0.0";

        private void Awake()
        {
            var log = new ModLog(Logger);
            log.LogInfo("RavenWatch proof of concept loaded.");
        }
    }
}
