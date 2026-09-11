using BepInEx;
using Landoria.SharedLib;

namespace Landoria.ModSentry
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ModSentryPlugin : LandoriaPlugin
    {
        internal const string InventoryRpc = "Landoria_ModSentry_Inventory";
        internal const string RejectionRpc = "Landoria_ModSentry_Rejection";
        internal const string RejectionAckRpc = "Landoria_ModSentry_RejectionAck";
        internal const int ProtocolVersion = 2;
        private const string PluginGuid = "Landoria.ModSentry";
        private const string PluginName = "Landoria.ModSentry";
        private const string PluginVersion = "1.0.18";

        internal static ModLog Log { get; private set; }
        internal static PluginPolicy Policy { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        internal static PluginPolicy EnsurePolicy()
        {
            if (Policy == null)
            {
                Policy = PluginPolicyLoader.Load();
                Log.LogInfo($"Loaded {Policy.Required.Count} required and " +
                            $"{Policy.Optional.Count} optional client mod policies.");
            }

            return Policy;
        }

        private void Update()
        {
            NonceHandshake.Tick();
            PendingDisconnects.Tick();
        }

        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            NonceHandshake.Clear();
            HandshakeState.Clear();
            PendingDisconnects.Clear();
            ClientMessage.Clear();
            Policy = null;
            ShutdownPlugin();
            Log = null;
        }
    }
}
