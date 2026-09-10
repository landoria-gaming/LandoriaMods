using HarmonyLib;
using Landoria.RavenWatch.Server.Inventory;
using Landoria.RavenWatch.Server.Journal;
using Landoria.RavenWatch.Server.Network;

namespace Landoria.RavenWatch.Server
{
    internal static class ServerRuntime
    {
        private static bool installed;
        internal static void Install(Harmony harmony)
        {
            if (installed) return;
            harmony.CreateClassProcessor(typeof(InventoryReceivePatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryCreatedZdoPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryDeserializedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryDestroyPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ReceivedRpcPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SentRpcPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RpcShutdownPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ServerConnectionPatch)).Patch();
            installed = true;
        }
    }
}
