using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(TextViewer), "LateUpdate")]
    internal static class TextViewerLateUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(TextViewer), "ShowText")]
    internal static class TextViewerShowTextServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(TextViewer), "Hide")]
    internal static class TextViewerHideServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(TextViewer), "HideIntro")]
    internal static class TextViewerHideIntroServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(TextViewer), "IsVisible")]
    internal static class TextViewerIsVisibleServerUiPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ServerUi.Disabled) return true;
            __result = false;
            return false;
        }
    }

}
