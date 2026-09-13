using HarmonyLib;

namespace Landoria.GentleDeath
{
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    internal static class CreateTombstonePatch
    {
        private static bool Prefix(Player __instance)
        {
            DeathInventory.CreateTombstone(__instance);
            return false;
        }
    }
}
