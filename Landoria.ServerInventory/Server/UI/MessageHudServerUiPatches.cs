using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(MessageHud), "Update")]
    internal static class MessageHudUpdateServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "ShowMessage")]
    internal static class MessageHudShowMessageServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "ShowBiomeFoundMsg")]
    internal static class MessageHudShowBiomeFoundMsgServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "QueueUnlockMsg")]
    internal static class MessageHudQueueUnlockMsgServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "ClearUnlockQueue")]
    internal static class MessageHudClearUnlockQueueServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "HideAll")]
    internal static class MessageHudHideAllServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "UpdateUnlockMsg")]
    internal static class MessageHudUpdateUnlockMsgServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "UpdateMessage")]
    internal static class MessageHudUpdateMessageServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "UpdateBiomeFound")]
    internal static class MessageHudUpdateBiomeFoundServerUiPatch
    {
        private static bool Prefix() => !ServerUi.Disabled;
    }

    [HarmonyPatch(typeof(MessageHud), "Start")]
    internal static class MessageHudStartServerUiPatch
    {
        private static bool Prefix()
        {
            if (!ServerUi.Disabled) return true;
            // Keep the routed endpoint without initializing local graphics.
            ZRoutedRpc.instance.Register<int, string>("ShowMessage", IgnoreMessage);
            return false;
        }

        private static void IgnoreMessage(long sender, int type, string text) { }
    }
}
