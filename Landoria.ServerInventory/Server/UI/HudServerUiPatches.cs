using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class HudUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "LateUpdate")]
    internal static class HudLateUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "ProfileSaveStarted")]
    internal static class HudProfileSaveStartedServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "ProfileSaveFinished")]
    internal static class HudProfileSaveFinishedServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "WorldSaveStarted")]
    internal static class HudWorldSaveStartedServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "WorldSaveFinished")]
    internal static class HudWorldSaveFinishedServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "StaggerBarFlash")]
    internal static class HudStaggerBarFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "DamageFlash")]
    internal static class HudDamageFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "FlashHealthBar")]
    internal static class HudFlashHealthBarServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "StaminaBarUppgradeFlash")]
    internal static class HudStaminaBarUppgradeFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "AdrenalineBarFlash")]
    internal static class HudAdrenalineBarFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "StaminaBarEmptyFlash")]
    internal static class HudStaminaBarEmptyFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "EitrBarEmptyFlash")]
    internal static class HudEitrBarEmptyFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "EitrBarUppgradeFlash")]
    internal static class HudEitrBarUppgradeFlashServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "TogglePieceSelection")]
    internal static class HudTogglePieceSelectionServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "HidePieceSelection")]
    internal static class HudHidePieceSelectionServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "CloseBuildUi")]
    internal static class HudCloseBuildUiServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(Hud), "IsVisible")]
    internal static class HudIsVisibleServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Hud), "IsQuickPieceSelectEnabled")]
    internal static class HudIsQuickPieceSelectEnabledServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Hud), "IsPieceSelectionVisible")]
    internal static class HudIsPieceSelectionVisibleServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Hud), "InRadial")]
    internal static class HudInRadialServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Hud), "InBuildUi")]
    internal static class HudInBuildUiServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

}
