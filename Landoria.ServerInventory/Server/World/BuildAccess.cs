using System;
using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(PrivateArea), "HaveLocalAccess")]
    internal static class BuildAccess
    {
        private static bool Prefix(PrivateArea __instance, ref bool __result)
        {
            var action = WorldActionTransaction.Current;
            if (action == null) return true;
            __result = __instance.GetComponent<Piece>().GetCreator() == action.PlayerId || IsPermitted(__instance, action.PlayerId);
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(PrivateArea), "IsPermitted")]
        private static bool IsPermitted(PrivateArea instance, long playerID)
            => throw new NotImplementedException("Native ward permission check was not patched.");
    }
}
