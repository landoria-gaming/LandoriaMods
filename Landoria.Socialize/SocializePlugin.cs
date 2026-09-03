using BepInEx;
using Landoria.SharedLib;

namespace Landoria.Socialize
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SocializePlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.Socialize";
        private const string PluginName = "Landoria.Socialize";
        private const string PluginVersion = "1.0.14";

        internal static ModLog Log { get; private set; }
        internal static SocializeSettings Settings { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            Settings = new SocializeSettings();
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void Update()
        {
            TextPermissionService.Update();
            GroupService.Update();
            TargetPingService.Update();
            PrivateChat.Update();
        }

        private void OnDestroy()
        {
            GroupService.Reset();
            TargetPingService.Reset();
            PrivateChat.Reset();
            TextPermissionService.Reset();
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Settings = null;
            Log = null;
        }
    }
}
