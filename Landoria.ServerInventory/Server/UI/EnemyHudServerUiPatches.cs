using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(EnemyHud), "LateUpdate")]
    internal static class EnemyHudLateUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
    internal static class EnemyHudShowHudServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(EnemyHud), "RemoveCharacterHud")]
    internal static class EnemyHudRemoveCharacterHudServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(EnemyHud), "ShowingBossHud")]
    internal static class EnemyHudShowingBossHudServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

}
