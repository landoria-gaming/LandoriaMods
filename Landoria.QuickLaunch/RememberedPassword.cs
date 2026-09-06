using System;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using GUIFramework;
using Landoria.SharedLib;

namespace Landoria.QuickLaunch
{
    internal static class RememberedPassword
    {
        internal const string Preference = "Landoria.QuickLaunch.LastPassword";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Landoria.QuickLaunch.Password.v1");
        private static string _savedCipher;
        private static string _candidateCipher;
        private static bool _activeAttempt;
        internal static ModLog Log { private get; set; }

        internal static void Reset()
        {
            _savedCipher = null;
            _candidateCipher = null;
            _activeAttempt = false;
        }

        internal static void BeginAttempt(bool restore)
        {
            Reset();
            _activeAttempt = true;
            if (restore && Environment.OSVersion.Platform == PlatformID.Win32NT)
                _savedCipher = PlatformPrefs.GetString(Preference);
        }

        internal static void SaveSuccessfulAttempt()
        {
            if (!string.IsNullOrEmpty(_candidateCipher))
                PlatformPrefs.SetString(Preference, _candidateCipher);
            Reset();
        }

        private static void Capture(ZNet network, string password)
        {
            if (!_activeAttempt || network.IsServer() || string.IsNullOrEmpty(password) ||
                Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            try
            {
                _candidateCipher = Convert.ToBase64String(
                    ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser));
            }
            catch (Exception exception)
            {
                Log.LogWarning($"QuickLaunch could not protect the server password: {exception}");
                _candidateCipher = null;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        private static string Restore()
        {
            byte[] bytes = null;
            try
            {
                bytes = ProtectedData.Unprotect(Convert.FromBase64String(_savedCipher),
                    Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"QuickLaunch could not restore the server password: {exception}");
                PlatformPrefs.DeleteKey(Preference);
                PlatformPrefs.Save();
                return null;
            }
            finally
            {
                if (bytes != null) Array.Clear(bytes, 0, bytes.Length);
                _savedCipher = null;
            }
        }

        [HarmonyPatch(typeof(ZNet), "SendPeerInfo")]
        private static class CapturePasswordPatch
        {
            private static void Prefix(ZNet __instance, string password) => Capture(__instance, password);
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        private static class SubmitPasswordPatch
        {
            private static void Postfix(ZNet __instance, bool needPassword)
            {
                if (!needPassword || !QuickLaunchPlugin.IsAutomaticLoading ||
                    string.IsNullOrEmpty(_savedCipher) || !__instance.InPasswordDialog()) return;
                string password = Restore();
                if (string.IsNullOrEmpty(password)) return;
                // Submit through Valheim's listener; it hashes the password using the server salt.
                __instance.m_passwordDialog.GetComponentInChildren<GuiInputField>()
                    .OnInputSubmit.Invoke(password);
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "ShowConnectError")]
        private static class InvalidPasswordPatch
        {
            private static void Prefix(ZNet.ConnectionStatus statusOverride)
            {
                ZNet.ConnectionStatus status = statusOverride == ZNet.ConnectionStatus.None
                    ? ZNet.GetConnectionStatus() : statusOverride;
                if (_activeAttempt && status == ZNet.ConnectionStatus.ErrorPassword)
                {
                    PlatformPrefs.DeleteKey(Preference);
                    PlatformPrefs.Save();
                }
                Reset();
            }
        }
    }
}
