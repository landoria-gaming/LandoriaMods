using System;
using BepInEx;
using Landoria.SharedLib;
using UnityEngine;

namespace Landoria.AfkDetector
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AfkDetectorPlugin : LandoriaPlugin
    {
        internal const string DisconnectReasonRpc = "Landoria_AfkDisconnectReason";
        private const string PluginGuid = "Landoria.AfkDetector";
        private const string PluginName = "Landoria.AfkDetector";
        private const string PluginVersion = "1.0.7";
        private const int DefaultTimeoutMinutes = 30;
        private const string TimeoutArgument = "--afktimeout";
        private const float DefaultMovementTolerance = 0.75f;
        private const float ScanIntervalSeconds = 2f;

        private int? _timeoutMinutes;
        private ActivityMonitor _monitor;
        private float _nextScan;
        internal static AfkDetectorPlugin Instance { get; private set; }
        internal static ModLog Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = InitializePlugin(PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} is loaded.");
        }

        private void Update()
        {
            if (!IsReadyDedicatedServer())
            {
                return;
            }
            InitializeServerTimeout();
            if (_timeoutMinutes == -1 || Time.unscaledTime < _nextScan)
            {
                return;
            }

            _nextScan = Time.unscaledTime + ScanIntervalSeconds;
            EnsureMonitor().Update(ZNet.instance.GetPeers(), Time.unscaledTime);
        }

        private ActivityMonitor EnsureMonitor()
        {
            float timeout = _timeoutMinutes.Value * 60f;
            if (_monitor == null)
            {
                _monitor = new ActivityMonitor(
                    timeout, DefaultMovementTolerance, DisconnectPlayer);
            }
            else
            {
                _monitor.Configure(timeout, DefaultMovementTolerance);
            }
            return _monitor;
        }

        private void InitializeServerTimeout()
        {
            if (_timeoutMinutes.HasValue || !IsReadyDedicatedServer())
            {
                return;
            }
            _timeoutMinutes = ReadCommandLineTimeout();
            Log.LogInfo(_timeoutMinutes == -1
                ? "AFK timeout is disabled."
                : $"AFK timeout is {_timeoutMinutes} minutes.");
        }

        private static int ReadCommandLineTimeout()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], TimeoutArgument,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return ParseCommandLineTimeout(arguments, index);
            }

            return DefaultTimeoutMinutes;
        }

        private static int ParseCommandLineTimeout(string[] arguments, int index)
        {
            if (index + 1 < arguments.Length &&
                int.TryParse(arguments[index + 1], out int minutes) &&
                (minutes == -1 || minutes >= 1))
            {
                Log.LogInfo($"Received command-line switch: {TimeoutArgument} {minutes}.");
                return minutes;
            }

            Log.LogWarning($"Invalid {TimeoutArgument} value; using {DefaultTimeoutMinutes} minutes.");
            return DefaultTimeoutMinutes;
        }

        internal void RecordChat(long peerId)
        {
            if (IsReadyDedicatedServer())
            {
                InitializeServerTimeout();
                if (_timeoutMinutes == -1)
                {
                    return;
                }
                EnsureMonitor().RecordChat(peerId, Time.unscaledTime);
            }
        }

        private static bool IsReadyDedicatedServer()
        {
            return ServerRole.IsDedicatedServer;
        }

        private static void DisconnectPlayer(ZNetPeer peer)
        {
            peer.m_rpc.Invoke(DisconnectReasonRpc, "Disconnected due to inactivity.");
            ZNet.instance.Kick(peer.m_socket.GetHostName());
            Log.LogInfo($"Requested inactivity disconnect for {peer.m_playerName}.");
        }

        private void OnDestroy()
        {
            Log?.LogInfo($"{PluginName} {PluginVersion} is unloaded.");
            ShutdownPlugin();
            _monitor = null;
            Instance = null;
            Log = null;
        }
    }
}
