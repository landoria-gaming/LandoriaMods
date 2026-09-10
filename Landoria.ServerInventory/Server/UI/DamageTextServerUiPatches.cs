using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(DamageText), "LateUpdate")]
    internal static class DamageTextLateUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(DamageText), "RPC_DamageText")]
    internal static class DamageTextRPC_DamageTextServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(DamageText), "AddInworldText")]
    internal static class DamageTextAddInworldTextServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

}
