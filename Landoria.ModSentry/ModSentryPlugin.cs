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
        internal const string AcceptanceRpc = "Landoria_ModSentry_Acceptance";
        internal const string CharacterPositionRpc =
            "Landoria_ModSentry_CharacterPosition";
        internal const string CheatDetectionRpc =
            "Landoria_ModSentry_CheatDetection";
        internal const string CheatDetectionEnableRpc =
            "Landoria_ModSentry_CheatDetectionEnable";
        internal const int ProtocolVersion = 1;
        public const int GuestControllerProtocolVersion =
            UnverifiedGuestControllerRegistry.ProtocolVersion;
        private const string PluginGuid = "Landoria.ModSentry";
        private const string PluginName = "Landoria.ModSentry";
        private const string PluginVersion = "1.0.14";

        internal static ModLog Log { get; private set; }
        internal static PluginPolicy Policy { get; private set; }

        public static void RegisterUnverifiedGuestController(
            IUnverifiedGuestController controller)
        {
            UnverifiedGuestControllerRegistry.Register(controller);
            System.Version version = controller.GetType().Assembly.GetName().Version;
            Log?.LogInfo(
                $"Registered the server-only unverified guest controller " +
                $"protocol {controller.ProtocolVersion}, assembly version {version}.");
        }

        public static void UnregisterUnverifiedGuestController(
            IUnverifiedGuestController controller)
        {
            UnverifiedGuestControllerRegistry.Unregister(controller);
            Log?.LogInfo("Unregistered the server-only unverified guest controller.");
        }

        public static bool TryGetLastVerifiedPosition(ZRpc rpc,
            out UnityEngine.Vector3 position)
        {
            return VerifiedCharacterPositions.TryGet(rpc, out position);
        }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            ModSentrySettings.Initialize();
            if (UnityEngine.Application.isBatchMode)
            {
                Log.LogInfo("Known managed cheat protection is " +
                    (ModSentrySettings.KnownCheatProtectionEnabled
                        ? "enabled." : "disabled."));
            }
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
            PendingDisconnects.Tick();
            ClientVerificationState.Update();
            ManagedCheatDetector.Update();
        }

        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            HandshakeState.Clear();
            PendingDisconnects.Clear();
            GuestAdmissions.Clear();
            ClientMessage.Clear();
            ClientVerificationState.Clear();
            VerifiedCharacterPositions.Clear();
            UnverifiedGuestControllerRegistry.Clear();
            ManagedCheatDetector.Shutdown();
            Policy = null;
            ShutdownPlugin();
            Log = null;
        }
    }
}
