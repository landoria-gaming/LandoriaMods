using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Landoria.SharedLib;

namespace Landoria.QuickLaunch
{
    // Manages automatic local and multiplayer sessions.
    internal static class QuickLaunchSession
    {
        private const string LastSessionPreference = "Landoria.QuickLaunch.LastSession";
        private const string LastServerPreference = "Landoria.QuickLaunch.LastServer";
        private const string LastServerNamePreference = "Landoria.QuickLaunch.LastServerName";
        private const string LastBackendPreference = "Landoria.QuickLaunch.LastBackend";
        private const string LastPlayFabHostPreference = "Landoria.QuickLaunch.LastPlayFabHost";
        private const string LocalSession = "local";
        private const string MultiplayerSession = "multiplayer";

        private static bool _attemptedAutoStart;
        private static bool _launchingAutomatically;
        private static ServerJoinData _joiningServer = ServerJoinData.None;

        internal static ModLog Log { get; set; }
        internal static bool IsAutomaticLoading { get; private set; }
        internal static string ConnectingServerName { get; private set; }

        // Starts the last saved session when the menu opens.
        internal static void StartLastSession(FejdStartup startup)
        {
            IsAutomaticLoading = false;
            RememberedPassword.Reset();
            if (_attemptedAutoStart)
            {
                return;
            }
            _attemptedAutoStart = true;
            if (CancelAutomaticStart())
            {
                return;
            }
            string profileFilename = PlatformPrefs.GetString("profile");
            if (!RememberedProfileExists(profileFilename))
            {
                return;
            }
            StartRememberedSession(startup, profileFilename);
        }

        // Records a local world start.
        internal static void RecordLocalSession()
        {
            Log.LogDebug("QuickLaunch FejdStartup.OnWorldStart prefix invoked.");
            IsAutomaticLoading = _launchingAutomatically;
            _joiningServer = ServerJoinData.None;
            ConnectingServerName = null;
            RememberedPassword.Reset();
            ClearSessionPreferences();
            PlatformPrefs.SetString(LastSessionPreference, LocalSession);
            PlatformPrefs.Save();
        }

        // Records a multiplayer server join.
        internal static void RecordMultiplayerSession(FejdStartup startup)
        {
            Log.LogDebug("QuickLaunch FejdStartup.JoinServer prefix invoked.");
            IsAutomaticLoading = _launchingAutomatically ||
                (IsAutomaticLoading && _joiningServer.IsValid &&
                 _joiningServer == startup.GetServerToJoin());
            if (startup.HasServerToJoin())
            {
                _joiningServer = startup.GetServerToJoin();
                RememberedPassword.BeginAttempt(IsAutomaticLoading && IsRememberedServer(_joiningServer));
                ConnectingServerName = GetConnectingServerName(_joiningServer);
            }
        }

        // Saves the server after a successful connection.
        internal static void RememberConnectedServer(ZNet network)
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
            {
                ConnectingServerName = serverName;
            }
            SaveConnectedServer(backend, network.GetWorldName());
        }

        // Chooses the saved local or multiplayer session.
        private static void StartRememberedSession(FejdStartup startup, string profileFilename)
        {
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

        // Stops automatic loading when Escape is held.
        private static bool CancelAutomaticStart()
        {
            if (!ZInput.GetKey(UnityEngine.KeyCode.Escape))
            {
                return false;
            }
            Log.LogDebug("QuickLaunch cancelled with Escape; saved session unchanged.");
            return true;
        }

        // Checks whether the saved character still exists.
        private static bool RememberedProfileExists(string profileFilename)
        {
            if (string.IsNullOrEmpty(profileFilename))
            {
                return false;
            }
            List<PlayerProfile> profiles = SaveSystem.GetAllPlayerProfiles();
            return profiles.Exists(profile => profile.GetFilename() == profileFilename);
        }

        // Checks whether the saved world still exists.
        private static bool RememberedWorldExists(string worldName)
        {
            if (string.IsNullOrEmpty(worldName))
            {
                return false;
            }
            List<World> worlds = SaveSystem.GetWorldList();
            return worlds.Exists(world => world.m_name == worldName);
        }

        // Prepares a connection to the last multiplayer server.
        private static void JoinLastMultiplayerSession(FejdStartup startup, string profileFilename)
        {
            ServerJoinData server;
            using (LocalServerList recentServers = new LocalServerList(
                       null, ServerListGui.GetServerListLocations("recent")))
            {
                if (recentServers.Count == 0 || !recentServers[0].IsValid)
                {
                    Log.LogError("QuickLaunch could not find a valid recent multiplayer server.");
                    return;
                }
                server = recentServers[0];
                startup.SetServerToJoin(server);
            }
            startup.StartCoroutine(JoinWhenReady(startup, server, profileFilename));
        }

        // Waits until the saved server is ready to join.
        private static IEnumerator JoinWhenReady(FejdStartup startup, ServerJoinData server,
            string profileFilename)
        {
            if (NeedsPlayFabLogin(server))
            {
                yield return WaitForPlayFab(startup, server);
            }
            if (startup == null || startup.GetServerToJoin() != server || CancelAutomaticStart() ||
                NeedsPlayFabLogin(server) && !PlayFabManager.IsLoggedIn)
            {
                yield break;
            }
            Log.LogDebug(
                $"QuickLaunch is joining multiplayer server '{server}' with character '{profileFilename}'.");
            startup.OnCharacterStart();
            RunAutomaticStart(startup.OnJoinStart);
        }

        // Checks whether a server uses PlayFab login.
        private static bool NeedsPlayFabLogin(ServerJoinData server) =>
            server.m_type.ToString() == nameof(ServerJoinDataType.PlayFabUser) ||
            server.m_type.ToString() == nameof(ServerJoinDataType.Dedicated);

        // Waits for PlayFab login to finish.
        private static IEnumerator WaitForPlayFab(FejdStartup startup, ServerJoinData server)
        {
            Log.LogDebug("QuickLaunch is waiting for PlayFab login in the background.");
            DateTime deadline = DateTime.UtcNow.AddSeconds(60);
            while (!PlayFabManager.IsLoggedIn)
            {
                if (CancelAutomaticStart() || startup == null || startup.GetServerToJoin() != server)
                {
                    yield break;
                }
                if (DateTime.UtcNow >= deadline)
                {
                    Log.LogWarning("QuickLaunch stopped waiting for PlayFab login; staying in the menu.");
                    yield break;
                }
                yield return null;
            }
        }

        // Runs a start action as an automatic launch.
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

        // Saves the details of a connected server.
        private static void SaveConnectedServer(OnlineBackendType backend, string worldName)
        {
            PlatformPrefs.SetString(LastServerNamePreference, ConnectingServerName ?? string.Empty);
            PlatformPrefs.SetString(LastPlayFabHostPreference,
                backend == OnlineBackendType.PlayFab ? ZNet.GetServerString(false) : string.Empty);
            PlatformPrefs.SetString(LastSessionPreference, MultiplayerSession);
            RememberedPassword.SaveSuccessfulAttempt();
            PlatformPrefs.Save();
            Log.LogDebug(
                $"QuickLaunch remembered connected server '{_joiningServer}' with backend '{backend}' " +
                $"and world '{worldName}'.");
            _joiningServer = ServerJoinData.None;
        }

        // Clears all saved session details.
        private static void ClearSessionPreferences()
        {
            string[] keys =
            {
                LastSessionPreference, LastServerPreference, LastServerNamePreference,
                LastBackendPreference, LastPlayFabHostPreference, "Landoria.QuickLaunch.LastWorld",
                RememberedPassword.Preference
            };
            foreach (string key in keys)
            {
                PlatformPrefs.DeleteKey(key);
            }
        }

        // Checks whether the saved world is selected.
        private static bool IsRememberedWorldSelected(FejdStartup startup, string worldName)
        {
            World selectedWorld = Traverse.Create(startup).Field("m_world").GetValue<World>();
            return selectedWorld != null && selectedWorld.m_name == worldName;
        }

        // Gets the display name of the server being joined.
        private static string GetConnectingServerName(ServerJoinData server)
        {
            if (MultiBackendMatchmaking.TryGetServerName(server, out string name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return IsRememberedServer(server) ? PlatformPrefs.GetString(LastServerNamePreference) : null;
        }

        // Checks whether a server matches the saved server.
        private static bool IsRememberedServer(ServerJoinData server)
        {
            bool sameServer = PlatformPrefs.GetString(LastServerPreference) == $"{server.m_type}:{server}";
            bool samePlayFabHost = server.m_type.ToString() == nameof(ServerJoinDataType.PlayFabUser) &&
                PlatformPrefs.GetString(LastBackendPreference) == OnlineBackendType.PlayFab.ToString() &&
                PlatformPrefs.GetString(LastPlayFabHostPreference) == server.PlayFabUser.m_remotePlayerId;
            return sameServer || samePlayFabHost;
        }

        // Starts the saved local world.
        private static void StartWorld(FejdStartup startup, string profileFilename, string worldName)
        {
            Log.LogDebug(
                $"QuickLaunch is joining local world '{worldName}' with character '{profileFilename}'.");
            RunAutomaticStart(startup.OnWorldStart);
        }
    }
}
