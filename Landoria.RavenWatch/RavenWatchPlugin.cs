using BepInEx;
using Landoria.SharedLib;
using Landoria.RavenWatch.Client;
using Landoria.RavenWatch.Server;
using UnityEngine;

namespace Landoria.RavenWatch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RavenWatchPlugin : LandoriaPlugin
    {
        private const string PluginGuid = "Landoria.RavenWatch";
        private const string PluginName = "Landoria.RavenWatch";
        private const string PluginVersion = "1.0.0";
        internal static ModLog Log { get; private set; }
        private bool server;

        private void Awake()
        {
            server = Application.isBatchMode;
            Log = server
                ? InitializePlugin(PluginGuid, ServerRuntime.PatchTypes)
                : InitializePlugin(PluginGuid);
            if (server) ServerRuntime.Open();
            else ClientRuntime.Open();
        }

        private void Update()
        {
            if (server) ServerRuntime.Tick();
            else ClientRuntime.Tick();
        }

        private void OnApplicationQuit()
        {
            if (server) ServerRuntime.Close();
            else ClientRuntime.Close();
        }

        private void OnDestroy()
        {
            if (server) ServerRuntime.Close();
            else ClientRuntime.Close();
            ShutdownPlugin();
            Log = null;
        }
    }
}
