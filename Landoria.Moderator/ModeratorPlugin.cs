using BepInEx;
using Landoria.SharedLib;
using System.Collections.Generic;

namespace Landoria.Moderator
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ModeratorPlugin : LandoriaPlugin
    {
        private static readonly HashSet<string> CommandsRequiringModerator =
            new HashSet<string>
            {
                "exploremap", "goto", "itemset", "playerlist", "summon",
                "resetmap", "spawn", "event", "stopevent"
            };

        internal static ModLog ModLogger { get; private set; }
        private const string PluginGuid = "Landoria.Moderator";
        private const string PluginName = "Landoria.Moderator";
        private const string PluginVersion = "1.0.8";

        private void Awake()
        {
            ModLogger = InitializePlugin(PluginGuid);
            RegisterCommands();
            ModLogger.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void OnDestroy()
        {
            ModeratorMapSharing.Disable();
            ModLogger?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            ModLogger = null;
        }

        private void Update()
        {
            PlayerPositionRpc.Update();
            ModeratorMapSharing.Update();
        }

        internal static void RegisterCommands()
        {
            ModeratorModeCommand.Register();
            ExploreMapCommand.Register();
            GotoCommand.Register();
            ItemSetCommand.Register();
            PlayerListCommand.Register();
            SummonCommand.Register();
            ResetMapCommand.Register();
            SpawnCommand.Register();
            EventCommands.Register();
        }

        internal static bool RequiresEnabledModerator(string command)
        {
            return CommandsRequiringModerator.Contains(command);
        }
    }
}
