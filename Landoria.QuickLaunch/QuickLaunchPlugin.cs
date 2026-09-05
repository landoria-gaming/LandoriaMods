using BepInEx;
using Landoria.SharedLib;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Landoria.QuickLaunch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class QuickLaunchPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.QuickLaunch";
        private const string PluginName = "Landoria.QuickLaunch";
        private const string PluginVersion = "1.0.7";
        private const string LastSessionPreference = "Landoria.QuickLaunch.LastSession";
        private const string LocalSession = "local";
        private const string MultiplayerSession = "multiplayer";

        private static ModLog ModLogger { get; set; }

        private static bool _attemptedAutoStart;
        private void Awake() 
        {
            ModLogger = InitializePlugin(PluginGuid);
            ModLogger.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void OnDestroy()
        {
            ModLogger?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            ModLogger = null;
        }

        private static void StartPostfix(FejdStartup startup)
        {
            if (_attemptedAutoStart || HasDisabledArgument())
            {
                return;
            }

            _attemptedAutoStart = true;
            string profileFilename = PlatformPrefs.GetString("profile");
            if (!RememberedProfileExists(profileFilename))
            {
                return;
            }

            if (PlatformPrefs.GetString(LastSessionPreference) == MultiplayerSession)
            {
                JoinLastMultiplayerSession(startup, profileFilename);
                return;
            }

            string worldName = PlatformPrefs.GetString("world");
            if (!RememberedWorldExists(worldName))
            {
                return;
            }

            startup.OnCharacterStart();
            if (IsRememberedWorldSelected(startup, worldName))
            {
                StartWorld(startup, profileFilename, worldName);
            }
        }

        private static bool HasDisabledArgument()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (MatchesDisabledArgument(arguments, index))
                {
                    ModLogger.LogDebug("Received command-line switch: --quicklaunch false.");
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesDisabledArgument(string[] arguments, int index)
        {
            return string.Equals(arguments[index], "--quicklaunch", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(arguments[index + 1], "false", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RememberedProfileExists(string profileFilename)
        {
            if (string.IsNullOrEmpty(profileFilename))
            {
                return false;
            }

            List<PlayerProfile> profiles = SaveSystem.GetAllPlayerProfiles();
            if (!profiles.Exists(profile => profile.GetFilename() == profileFilename))
            {
                return false;
            }

            return true;
        }

        private static bool RememberedWorldExists(string worldName)
        {
            if (string.IsNullOrEmpty(worldName))
            {
                return false;
            }

            List<World> worlds = SaveSystem.GetWorldList();
            if (worlds.Exists(world => world.m_name == worldName))
            {
                return true;
            }

            return false;
        }

        private static void JoinLastMultiplayerSession(FejdStartup startup, string profileFilename)
        {
            ServerJoinData server;
            using (LocalServerList recentServers = new LocalServerList(
                       null, ServerListGui.GetServerListLocations("recent")))
            {
                if (recentServers.Count == 0 || !recentServers[0].IsValid)
                {
                    ModLogger.LogError("QuickLaunch could not find a valid recent multiplayer server.");
                    return;
                }

                server = recentServers[0];
                startup.SetServerToJoin(server);
            }

            ModLogger.LogDebug(
                $"QuickLaunch is joining multiplayer server '{server}' with character '{profileFilename}'.");
            startup.OnCharacterStart();
            startup.JoinServer();
        }

        private static bool IsRememberedWorldSelected(FejdStartup startup, string worldName)
        {
            World selectedWorld = Traverse.Create(startup).Field("m_world").GetValue<World>();
            if (selectedWorld != null && selectedWorld.m_name == worldName)
            {
                return true; 
            }

            return false;
        }

        private static void StartWorld(FejdStartup startup, string profileFilename, string worldName)
        {
            ModLogger.LogDebug(
                $"QuickLaunch is joining local world '{worldName}' with character '{profileFilename}'.");
            startup.OnWorldStart();
        }

        [HarmonyPatch(typeof(FejdStartup), "Start")]
        private static class StartPatch
        {
            private static void Postfix(FejdStartup __instance)
            {
                ModLogger.LogDebug("QuickLaunch FejdStartup.Start postfix invoked.");
                StartPostfix(__instance);
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "OnWorldStart")]
        private static class LocalSessionPatch
        {
            private static void Prefix()
            {
                ModLogger.LogDebug("QuickLaunch FejdStartup.OnWorldStart prefix invoked.");
                PlatformPrefs.SetString(LastSessionPreference, LocalSession);
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "JoinServer")]
        private static class MultiplayerSessionPatch
        {
            private static void Prefix(FejdStartup __instance)
            {
                ModLogger.LogDebug("QuickLaunch FejdStartup.JoinServer prefix invoked.");
                if (__instance.HasServerToJoin())
                {
                    PlatformPrefs.SetString(LastSessionPreference, MultiplayerSession);
                }
            }
        }
    }
}
