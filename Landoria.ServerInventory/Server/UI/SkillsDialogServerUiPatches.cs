using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(SkillsDialog), "Update")]
    internal static class SkillsDialogUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(SkillsDialog), "Setup")]
    internal static class SkillsDialogSetupServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(SkillsDialog), "OnClose")]
    internal static class SkillsDialogOnCloseServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(SkillsDialog), "SkillClicked")]
    internal static class SkillsDialogSkillClickedServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

}
