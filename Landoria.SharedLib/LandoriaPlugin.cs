using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace Landoria.SharedLib
{
    public abstract class LandoriaPlugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private bool _patchesApplied;

        protected ModLog InitializePlugin(string pluginGuid)
        {
            ModLog log = new ModLog(Logger);
            System.Version assemblyVersion = GetType().Assembly.GetName().Version;
            log.LogInfo($"AssemblyVersion: {assemblyVersion}.");
            EnsureSharedPatches(log);
            _harmony = new Harmony(pluginGuid);
            PatchOwnNamespace(log);
            return log;
        }

        private static void EnsureSharedPatches(ModLog log)
        {
            const string key = "Landoria.SharedLib.ConnectionFailureMenuPatch.v1";
            lock (AppDomain.CurrentDomain)
            {
                if (AppDomain.CurrentDomain.GetData(key) != null) return;
                new Harmony("Landoria.SharedLib")
                    .CreateClassProcessor(typeof(ConnectionFailureMenuPatch)).Patch();
                AppDomain.CurrentDomain.SetData(key, true);
                log.LogDebug("Shared connection failure menu patch was applied.");
            }
        }

        protected void PatchOwnNamespace(ModLog log)
        {
            if (_patchesApplied)
            {
                log.LogDebug("Harmony patches are already active; skipping registration.");
                return;
            }

            string pluginNamespace = GetType().Namespace;
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Namespace == pluginNamespace)
                {
                    _harmony.CreateClassProcessor(type).Patch();
                }
            }

            _patchesApplied = true;
            log.LogDebug("Harmony patches were applied for the plugin namespace.");
        }

        protected void ShutdownPlugin()
        {
            if (!_patchesApplied)
            {
                return;
            }

            _harmony?.UnpatchSelf();
            _patchesApplied = false;
        }
    }
}
