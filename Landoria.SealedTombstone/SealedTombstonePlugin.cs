using BepInEx;
using Landoria.SharedLib;

namespace Landoria.SealedTombstone
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SealedTombstonePlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.SealedTombstone";
        private const string PluginName = "Landoria.SealedTombstone";
        private const string PluginVersion = "1.0.9";


        internal static ModLog Log { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void Update()
        {
            TombstoneAccess.Tick();
        }

        private void OnDestroy()
        {
            TombstoneAccess.ResetSession();
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            Log = null;
        }
    }
}
