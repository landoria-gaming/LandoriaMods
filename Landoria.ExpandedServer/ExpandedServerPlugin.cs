using System;
using BepInEx;
using Landoria.SharedLib;

namespace Landoria.ExpandedServer
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ExpandedServerPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.ExpandedServer";
        private const string PluginName = "Landoria.ExpandedServer";
        private const string PluginVersion = "1.0.10";
        private const int DefaultMaxPlayers = 20;
        private const int MaximumMaxPlayers = 100;

        internal static int MaxPlayers { get; private set; } = DefaultMaxPlayers;
        internal static uint PlayFabCapacity => (uint)(MaxPlayers + 1);
        internal static bool IsLocalServer => ZNet.instance != null && ZNet.instance.IsServer();
        private static bool settingsInitialized;

        internal static ModLog Log { get; private set; }

        private void Awake()
        {
            Log = InitializePlugin(PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void Update()
        {
            InitializeDedicatedServerSettings();
        }

        internal static void InitializeDedicatedServerSettings()
        {
            if (settingsInitialized || !ServerRole.IsDedicatedServer) return;
            MaxPlayers = ReadMaxPlayers();
            settingsInitialized = true;
            Log.LogInfo($"Dedicated server player limit is {MaxPlayers}.");
        }

        private static int ReadMaxPlayers()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], "--maxplayer",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (index + 1 >= arguments.Length ||
                    !int.TryParse(arguments[index + 1], out int requested) || requested < 1)
                {
                    Log.LogWarning("Invalid --maxplayer value; using the default limit of 20.");
                    return DefaultMaxPlayers;
                }

                int limited = Math.Min(requested, MaximumMaxPlayers);
                Log.LogDebug($"Received command-line switch: --maxplayer {requested}; limit={limited}.");
                return limited;
            }

            Log.LogDebug("No --maxplayer switch was provided; using the default limit of 20.");
            return DefaultMaxPlayers;
        }

        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            MaxPlayers = DefaultMaxPlayers;
            settingsInitialized = false;
            Log = null;
        }
    }
}
