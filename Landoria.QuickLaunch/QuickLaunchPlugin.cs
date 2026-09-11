using BepInEx;
using Landoria.SharedLib;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Landoria.QuickLaunch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class QuickLaunchPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.QuickLaunch";
        private const string PluginName = "Landoria.QuickLaunch";
        private const string PluginVersion = "1.0.9";
        private const string LastSessionPreference = "Landoria.QuickLaunch.LastSession";
        private const string LastServerPreference = "Landoria.QuickLaunch.LastServer";
        private const string LastServerNamePreference = "Landoria.QuickLaunch.LastServerName";
        private const string LastBackendPreference = "Landoria.QuickLaunch.LastBackend";
        private const string LastPlayFabHostPreference = "Landoria.QuickLaunch.LastPlayFabHost";
        private const string LocalSession = "local";
        private const string MultiplayerSession = "multiplayer";

        private static ModLog ModLogger { get; set; }

        private static bool _attemptedAutoStart;
        private static bool _launchingAutomatically;
        internal static bool IsAutomaticLoading { get; private set; }
        private static ServerJoinData _joiningServer = ServerJoinData.None;
        internal static string ConnectingServerName { get; private set; }
        private void Awake() 
        {
            ModLogger = InitializePlugin(PluginGuid);
            RememberedPassword.Log = ModLogger;
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
            IsAutomaticLoading = false;
            RememberedPassword.Reset();
            if (_attemptedAutoStart)
            {
                return;
            }

            _attemptedAutoStart = true;
            if (CancelAutomaticStart()) return;
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

        private static bool CancelAutomaticStart()
        {
            if (!ZInput.GetKey(UnityEngine.KeyCode.Escape)) return false;
            ModLogger.LogDebug("QuickLaunch cancelled with Escape; saved session unchanged.");
            return true;
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

            startup.StartCoroutine(JoinWhenReady(startup, server, profileFilename));
        }

        private static IEnumerator JoinWhenReady(FejdStartup startup, ServerJoinData server,
            string profileFilename)
        {
            // Xbox omits SteamUser, so the numeric enum values differ from Steam.
            if (server.m_type.ToString() == nameof(ServerJoinDataType.PlayFabUser) ||
                server.m_type.ToString() == nameof(ServerJoinDataType.Dedicated))
            {
                ModLogger.LogDebug("QuickLaunch is waiting for PlayFab login in the background.");
                DateTime deadline = DateTime.UtcNow.AddSeconds(60);
                while (!PlayFabManager.IsLoggedIn)
                {
                    if (CancelAutomaticStart()) yield break;
                    if (startup == null || startup.GetServerToJoin() != server) yield break;
                    if (DateTime.UtcNow >= deadline)
                    {
                        ModLogger.LogWarning("QuickLaunch stopped waiting for PlayFab login; staying in the menu.");
                        yield break;
                    }
                    yield return null;
                }
            }

            if (startup == null || startup.GetServerToJoin() != server) yield break;
            if (CancelAutomaticStart()) yield break;
            ModLogger.LogDebug(
                $"QuickLaunch is joining multiplayer server '{server}' with character '{profileFilename}'.");
            startup.OnCharacterStart();
            RunAutomaticStart(startup.OnJoinStart);
        }

        private static void RunAutomaticStart(Action start)
        {
            _launchingAutomatically = true;
            try
            {
                start();
            }
            finally
            {
                _launchingAutomatically = false;
            }
        }

        private static void RememberConnectedServer(ZNet network)
        {
            if (network.IsServer() || !_joiningServer.IsValid ||
                ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
            {
                return;
            }

            OnlineBackendType backend = ZNet.m_onlineBackend;
            ClearSessionPreferences();
            PlatformPrefs.SetString(LastServerPreference, $"{_joiningServer.m_type}:{_joiningServer}");
            PlatformPrefs.SetString(LastBackendPreference, backend.ToString());
            if (MultiBackendMatchmaking.TryGetServerName(_joiningServer, out string serverName))
                ConnectingServerName = serverName;
            PlatformPrefs.SetString(LastServerNamePreference, ConnectingServerName ?? string.Empty);
            PlatformPrefs.SetString(LastPlayFabHostPreference,
                backend == OnlineBackendType.PlayFab ? ZNet.GetServerString(false) : string.Empty);
            PlatformPrefs.SetString(LastSessionPreference, MultiplayerSession);
            RememberedPassword.SaveSuccessfulAttempt();
            PlatformPrefs.Save();
            ModLogger.LogDebug(
                $"QuickLaunch remembered connected server '{_joiningServer}' with backend '{backend}' " +
                $"and world '{network.GetWorldName()}'.");
            _joiningServer = ServerJoinData.None;
        }

        private static void ClearSessionPreferences()
        {
            string[] keys =
            {
                LastSessionPreference, LastServerPreference, LastServerNamePreference,
                LastBackendPreference, LastPlayFabHostPreference, "Landoria.QuickLaunch.LastWorld",
                RememberedPassword.Preference
            };
            foreach (string key in keys)
                PlatformPrefs.DeleteKey(key);
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

        private static string GetConnectingServerName(ServerJoinData server)
        {
            if (MultiBackendMatchmaking.TryGetServerName(server, out string name) &&
                !string.IsNullOrWhiteSpace(name)) return name;
            return IsRememberedServer(server) ? PlatformPrefs.GetString(LastServerNamePreference) : null;
        }

        private static bool IsRememberedServer(ServerJoinData server)
        {
            bool sameServer = PlatformPrefs.GetString(LastServerPreference) == $"{server.m_type}:{server}";
            bool samePlayFabHost = server.m_type.ToString() == nameof(ServerJoinDataType.PlayFabUser) &&
                PlatformPrefs.GetString(LastBackendPreference) == OnlineBackendType.PlayFab.ToString() &&
                PlatformPrefs.GetString(LastPlayFabHostPreference) == server.PlayFabUser.m_remotePlayerId;
            return sameServer || samePlayFabHost;
        }

        private static void StartWorld(FejdStartup startup, string profileFilename, string worldName)
        {
            ModLogger.LogDebug(
                $"QuickLaunch is joining local world '{worldName}' with character '{profileFilename}'.");
            RunAutomaticStart(startup.OnWorldStart);
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
                IsAutomaticLoading = _launchingAutomatically;
                _joiningServer = ServerJoinData.None;
                ConnectingServerName = null;
                RememberedPassword.Reset();
                ClearSessionPreferences();
                PlatformPrefs.SetString(LastSessionPreference, LocalSession);
                PlatformPrefs.Save();
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "JoinServer")]
        private static class MultiplayerSessionPatch
        {
            private static void Prefix(FejdStartup __instance)
            {
                ModLogger.LogDebug("QuickLaunch FejdStartup.JoinServer prefix invoked.");
                // Valheim can resume this same join after authentication or a privilege prompt.
                IsAutomaticLoading = _launchingAutomatically ||
                    (IsAutomaticLoading && _joiningServer.IsValid &&
                     _joiningServer == __instance.GetServerToJoin());
                if (__instance.HasServerToJoin())
                {
                    _joiningServer = __instance.GetServerToJoin();
                    RememberedPassword.BeginAttempt(IsAutomaticLoading && IsRememberedServer(_joiningServer));
                    ConnectingServerName = GetConnectingServerName(_joiningServer);
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        private static class ConnectedServerPatch
        {
            private static void Postfix(ZNet __instance)
            {
                RememberConnectedServer(__instance);
            }
        }
    }
}
