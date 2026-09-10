using BepInEx;
using HarmonyLib;
using Landoria.SharedLib;
using Landoria.ServerInventory.Client;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ServerInventoryPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "Landoria.ServerInventory";
        private const string PluginName = "Landoria.ServerInventory";
        private const string PluginVersion = "1.0.0";
        private Harmony harmony;

        private void Awake()
        {

            CharacterSavePatch.Log = new ModLog(Logger);
            harmony = new Harmony(PluginGuid);
            CharacterRpc.Log = CharacterSavePatch.Log;
            harmony.PatchAll(typeof(ServerInventoryPlugin).Assembly);
            CharacterSavePatch.Log.LogInfo("Existing client character files will no longer be updated.");
        }

        private void Update()
        {
            Server.ServerRespawn.Tick();
            Server.ServerAttacks.Tick();
            Server.NativeDamage.Tick();
            Server.PendingPickups.Tick();
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}
