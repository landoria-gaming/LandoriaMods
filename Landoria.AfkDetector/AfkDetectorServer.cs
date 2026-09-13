using System;
using Landoria.SharedLib;
using UnityEngine;

namespace Landoria.AfkDetector
{
    // Runs AFK detection on the dedicated server.
    internal static class AfkDetectorServer
    {
        private const int DefaultTimeoutMinutes = 30;
        private const string TimeoutArgument = "--afktimeout";
        private const float MovementTolerance = 0.75f;
        private const float ScanIntervalSeconds = 2f;
        private static int? _timeoutMinutes;
        private static ActivityMonitor _monitor;
        private static float _nextScan;

        internal static bool IsReady => ServerRole.IsDedicatedServer;

        // Resets the server-side AFK state.
        internal static void Start()
        {
            Stop();
        }

        // Checks player activity at regular intervals.
        internal static void Tick()
        {
            if (!IsReady)
            {
                return;
            }

            InitializeTimeout();
            if (_timeoutMinutes == -1 || Time.unscaledTime < _nextScan)
            {
                return;
            }

            _nextScan = Time.unscaledTime + ScanIntervalSeconds;
            EnsureMonitor().Update(ZNet.instance.GetPeers(), Time.unscaledTime);
        }

        // Records chat activity for a player.
        internal static void RecordChat(long peerId)
        {
            if (!IsReady)
            {
                return;
            }

            InitializeTimeout();
            if (_timeoutMinutes != -1)
            {
                EnsureMonitor().RecordChat(peerId, Time.unscaledTime);
            }
        }

        // Clears the server-side AFK state.
        internal static void Stop()
        {
            _timeoutMinutes = null;
            _monitor = null;
            _nextScan = 0f;
        }

        // Creates or updates the activity monitor.
        private static ActivityMonitor EnsureMonitor()
        {
            float timeout = _timeoutMinutes.Value * 60f;
            if (_monitor == null)
            {
                _monitor = new ActivityMonitor(timeout, MovementTolerance, DisconnectPlayer);
            }
            else
            {
                _monitor.Configure(timeout, MovementTolerance);
            }

            return _monitor;
        }

        // Reads the timeout when the server is ready.
        private static void InitializeTimeout()
        {
            if (_timeoutMinutes.HasValue)
            {
                return;
            }

            _timeoutMinutes = ReadTimeout();
            AfkDetectorPlugin.Log.LogInfo(_timeoutMinutes == -1
                ? "AFK timeout is disabled."
                : $"AFK timeout is {_timeoutMinutes} minutes.");
        }

        // Finds the timeout in the command-line arguments.
        private static int ReadTimeout()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], TimeoutArgument,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ParseTimeout(arguments, index);
                }
            }

            return DefaultTimeoutMinutes;
        }

        // Validates the command-line timeout value.
        private static int ParseTimeout(string[] arguments, int index)
        {
            if (index + 1 < arguments.Length &&
                int.TryParse(arguments[index + 1], out int minutes) &&
                (minutes == -1 || minutes >= 1))
            {
                AfkDetectorPlugin.Log.LogInfo(
                    $"Received command-line switch: {TimeoutArgument} {minutes}.");
                return minutes;
            }

            AfkDetectorPlugin.Log.LogWarning(
                $"Invalid {TimeoutArgument} value; using {DefaultTimeoutMinutes} minutes.");
            return DefaultTimeoutMinutes;
        }

        // Disconnects a player who stayed inactive too long.
        private static void DisconnectPlayer(ZNetPeer peer)
        {
            peer.m_rpc.Invoke(ClientDisconnectReason.RpcName,
                "Disconnected due to inactivity.");
            ZNet.instance.Kick(peer.m_socket.GetHostName());
            AfkDetectorPlugin.Log.LogInfo(
                $"Requested inactivity disconnect for {peer.m_playerName}.");
        }
    }
}
