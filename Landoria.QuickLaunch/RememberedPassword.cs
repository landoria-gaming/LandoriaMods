using System;
using System.Security.Cryptography;
using System.Text;
using Landoria.SharedLib;

namespace Landoria.QuickLaunch
{
    // Saves and restores the password for the last server.
    internal static class RememberedPassword
    {
        internal const string Preference = "Landoria.QuickLaunch.LastPassword";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Landoria.QuickLaunch.Password.v1");
        private static string _savedCipher;
        private static string _candidateCipher;
        private static bool _activeAttempt;
        internal static ModLog Log { private get; set; }
        internal static bool HasSavedPassword => !string.IsNullOrEmpty(_savedCipher);

        // Clears the current password attempt.
        internal static void Reset()
        {
            _savedCipher = null;
            _candidateCipher = null;
            _activeAttempt = false;
        }

        // Starts tracking a server connection attempt.
        internal static void BeginAttempt(bool restore)
        {
            Reset();
            _activeAttempt = true;
            if (restore && Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _savedCipher = PlatformPrefs.GetString(Preference);
            }
        }

        // Saves the password after a successful connection.
        internal static void SaveSuccessfulAttempt()
        {
            if (!string.IsNullOrEmpty(_candidateCipher))
            {
                PlatformPrefs.SetString(Preference, _candidateCipher);
            }
            Reset();
        }

        // Protects a password used during a connection attempt.
        internal static void Capture(ZNet network, string password)
        {
            if (!_activeAttempt || network.IsServer() || string.IsNullOrEmpty(password) ||
                Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }
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

        // Restores the saved password for a connection.
        internal static string Restore()
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
                if (bytes != null)
                {
                    Array.Clear(bytes, 0, bytes.Length);
                }
                _savedCipher = null;
            }
        }

        // Clears saved data after a connection error.
        internal static void HandleConnectionError(ZNet.ConnectionStatus status)
        {
            if (_activeAttempt && status == ZNet.ConnectionStatus.ErrorPassword)
            {
                PlatformPrefs.DeleteKey(Preference);
                PlatformPrefs.Save();
            }
            Reset();
        }

    }
}
