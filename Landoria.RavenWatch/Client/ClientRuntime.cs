using HarmonyLib;

namespace Landoria.RavenWatch.Client
{
    internal static class ClientRuntime
    {
        private static bool installed;
        internal static void Install(Harmony harmony)
        {
            if (installed) return;
            harmony.CreateClassProcessor(typeof(InventoryChangePatch0)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch1)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch2)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch3)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch4)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch5)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch6)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch7)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch8)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch9)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch10)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch11)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch12)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch13)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch14)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch15)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch16)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangePatch17)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangeSuppression0)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangeSuppression1)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryChangeSuppression2)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryEntryAddPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryEntryPositionPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryEntrySlotPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryEntryLoadPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryEntryTemporaryLoadPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InventoryEntryMovePatch)).Patch();
            harmony.CreateClassProcessor(typeof(ItemBrokenPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ItemCraftedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ClientConnectionPatch)).Patch();
            installed = true;
        }
    }
}
