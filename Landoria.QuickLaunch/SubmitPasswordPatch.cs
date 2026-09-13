using GUIFramework;
using HarmonyLib;

namespace Landoria.QuickLaunch
{
    // Submits the saved password when the server asks for it.
    [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
    internal static class SubmitPasswordPatch
    {
        // Submits the password after the server handshake.
        private static void Postfix(ZNet __instance, bool needPassword)
        {
            if (!needPassword || !QuickLaunchSession.IsAutomaticLoading ||
                !RememberedPassword.HasSavedPassword || !__instance.InPasswordDialog())
            {
                return;
            }
            string password = RememberedPassword.Restore();
            if (string.IsNullOrEmpty(password))
            {
                return;
            }
            // Submit through Valheim's listener; it hashes the password using the server salt.
            __instance.m_passwordDialog.GetComponentInChildren<GuiInputField>()
                .OnInputSubmit.Invoke(password);
        }
    }
}
