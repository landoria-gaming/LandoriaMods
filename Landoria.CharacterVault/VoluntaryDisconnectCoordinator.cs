using System;
using System.Collections;
using UnityEngine;

namespace Landoria.CharacterVault
{
    internal sealed class VoluntaryDisconnectCoordinator : IDisposable
    {
        private const float ConfirmationTimeoutSeconds = 10;
        private bool _allowApplicationQuit;
        private bool _allowLogout;
        private bool _allowLogoutPrompt;
        private bool _logoutPromptScheduled;
        private bool _allowShutdown;
        private Game _game;
        private Menu _menu;
        private bool _logoutSave;
        private bool _logoutStartScene;
        private string _requestId;
        private VoluntaryExitKind _exitKind;
        private bool _playerEnteredWorld;

        internal VoluntaryDisconnectCoordinator()
        {
            Application.wantsToQuit += AllowApplicationQuit;
        }

        internal bool HasPendingSave => _requestId != null;

        internal bool AllowLogoutPrompt(Menu menu)
        {
            if (_allowLogoutPrompt)
            {
                _allowLogoutPrompt = false;
                CharacterVaultPlugin.Log.LogInfo(
                    "Allowing the vanilla disconnect confirmation after the final character save was accepted.");
                return true;
            }

            if (_logoutPromptScheduled)
            {
                CharacterVaultPlugin.Log.LogInfo(
                    "Ignoring a repeated Disconnect click while the confirmed dialog is scheduled.");
                return false;
            }

            bool delayed = Start(VoluntaryExitKind.LogoutPrompt, Game.instance, true, true);
            if (delayed)
            {
                _menu = menu;
                CharacterVaultPlugin.Log.LogMessage(
                    "Intercepted the Disconnect button before its confirmation dialog; waiting for the final save acceptance.");
            }
            return !delayed;
        }

        internal bool AllowLogout(Game game, bool save, bool changeToStartScene)
        {
            if (_allowLogout)
            {
                _allowLogout = false;
                CharacterVaultPlugin.Log.LogInfo(
                    "Allowing Game.Logout after the final character save was accepted.");
                return true;
            }

            return !Start(VoluntaryExitKind.Logout, game, save, changeToStartScene);
        }

        internal void RecordSaveCommitted(string requestId)
        {
            if (requestId != _requestId)
            {
                return;
            }

            CharacterVaultPlugin.Log.LogMessage(
                $"Final voluntary disconnect save {requestId} accepted.");
            CharacterVaultPlugin.Transfers.SuppressRedundantDisconnectUpload();
            CompletePendingExit("after the confirmed save");
        }

        internal void RecordConnectionStarted()
        {
            _allowLogout = false;
            _playerEnteredWorld = false;
            ClearPendingRequest();
        }

        internal void RecordPlayerSpawned()
        {
            _playerEnteredWorld = true;
            CharacterVaultPlugin.Log.LogInfo(
                "CharacterVault final-save protection armed after the local player spawned.");
        }

        internal bool AllowShutdown(Game game, bool saveWorld)
        {
            if (_allowShutdown)
            {
                _allowShutdown = false;
                CharacterVaultPlugin.Log.LogInfo(
                    "Allowing shutdown after the final character save was accepted.");
                return true;
            }

            return !saveWorld || !Start(VoluntaryExitKind.Logout, game, true, true);
        }

        internal void RecordConnectionLost()
        {
            if (_requestId == null)
            {
                return;
            }

            CharacterVaultPlugin.Log.LogWarning(
                $"Connection was lost while final save {_requestId} was pending; confirmation is impossible.");
            ClearPendingRequest();
            _playerEnteredWorld = false;
        }

        internal bool AllowMenuQuit()
        {
            if (_allowApplicationQuit)
            {
                return true;
            }

            bool delayed = Start(VoluntaryExitKind.ApplicationQuit, Game.instance, true, false);
            if (delayed)
            {
                CharacterVaultPlugin.Log.LogMessage(
                    "Intercepted the in-game Quit action; waiting for the final save acceptance.");
            }
            return !delayed;
        }

        internal void HandleNativeCloseRequest()
        {
            bool delayed = Start(VoluntaryExitKind.ApplicationQuit, Game.instance, true, false);
            if (delayed)
            {
                CharacterVaultPlugin.Log.LogMessage(
                    "Intercepted the Windows close action; waiting for the final save acceptance.");
                return;
            }

            CharacterVaultPlugin.WindowsClose?.AuthorizeClose();
            Application.Quit();
        }

        public void Dispose()
        {
            Application.wantsToQuit -= AllowApplicationQuit;
            ClearPendingRequest();
        }

        private bool AllowApplicationQuit()
        {
            if (_allowApplicationQuit)
            {
                CharacterVaultPlugin.Log.LogInfo("Application quit authorization consumed.");
                return true;
            }

            return !Start(VoluntaryExitKind.ApplicationQuit, Game.instance, true, false);
        }

        private bool Start(VoluntaryExitKind kind, Game game, bool save, bool startScene)
        {
            string requestId = "disconnect-" + Guid.NewGuid().ToString("N");
            IVoluntaryExitSaveRequest request = new VoluntaryExitSaveRequest(() =>
                CharacterVaultPlugin.Transfers?.BeginFinalDisconnectSave(requestId) == true);
            VoluntaryExitSaveAction action = VoluntaryExitSavePolicy.Start(
                _playerEnteredWorld, _requestId != null, request);
            if (action != VoluntaryExitSaveAction.WaitForNewSave)
            {
                return action == VoluntaryExitSaveAction.WaitForPendingSave;
            }

            _requestId = requestId;
            _exitKind = kind;
            _game = game;
            _logoutSave = save;
            _logoutStartScene = startScene;
            CharacterVaultPlugin.Log.LogMessage(
                $"Delayed voluntary {Describe(kind)} until final save {requestId} is committed.");
            CharacterVaultPlugin.Instance.Run(WaitForConfirmation(requestId));
            return true;
        }

        private IEnumerator WaitForConfirmation(string requestId)
        {
            float deadline = Time.realtimeSinceStartup + ConfirmationTimeoutSeconds;
            while (_requestId == requestId && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (_requestId != requestId)
            {
                yield break;
            }

            CharacterVaultPlugin.Log.LogError(
                $"Allowing voluntary {Describe(_exitKind)} because final save {requestId} " +
                $"was not confirmed within {ConfirmationTimeoutSeconds:0} seconds.");
            CompletePendingExit("after the confirmation timeout");
        }

        private void CompletePendingExit(string reason)
        {
            VoluntaryExitKind exitKind = _exitKind;
            Game game = _game;
            Menu menu = _menu;
            bool logoutSave = _logoutSave;
            bool logoutStartScene = _logoutStartScene;
            ClearPendingRequest();
            if (exitKind == VoluntaryExitKind.LogoutPrompt)
            {
                if (menu == null)
                {
                    CharacterVaultPlugin.Log.LogWarning(
                        "Canceled the disconnect confirmation because the Menu instance was destroyed.");
                    return;
                }

                _logoutPromptScheduled = true;
                CharacterVaultPlugin.Log.LogInfo(
                    $"Scheduling the unmodified vanilla disconnect confirmation one second {reason}.");
                CharacterVaultPlugin.Instance.Run(
                    OpenLogoutPromptAfterDelay(menu, reason));
                return;
            }

            if (exitKind == VoluntaryExitKind.ApplicationQuit)
            {
                _allowApplicationQuit = true;
                CharacterVaultPlugin.WindowsClose?.AuthorizeClose();
                CharacterVaultPlugin.Log.LogInfo($"Allowing application quit {reason}.");
                Application.Quit();
                return;
            }

            CharacterVaultPlugin.Log.LogInfo(
                $"Deferring the vanilla Game.Logout until the next Unity frame {reason}.");
            CharacterVaultPlugin.Instance.Run(
                LogoutNextFrame(game, logoutSave, logoutStartScene, reason));
        }

        private IEnumerator LogoutNextFrame(Game game, bool save, bool startScene, string reason)
        {
            yield return null;
            if (game == null)
            {
                CharacterVaultPlugin.Log.LogWarning(
                    "Canceled the deferred Game.Logout because the Game instance was destroyed.");
                yield break;
            }

            _allowLogout = true;
            _allowShutdown = true;
            CharacterVaultPlugin.Log.LogInfo(
                $"Calling the unmodified vanilla Game.Logout on the next Unity frame {reason}.");
            game.Logout(save, startScene);
        }

        private IEnumerator OpenLogoutPromptAfterDelay(Menu menu, string reason)
        {
            yield return new WaitForSecondsRealtime(1f);
            _logoutPromptScheduled = false;
            if (menu == null)
            {
                CharacterVaultPlugin.Log.LogWarning(
                    "Canceled the delayed disconnect confirmation because the Menu instance was destroyed.");
                yield break;
            }

            _allowLogoutPrompt = true;
            CharacterVaultPlugin.Log.LogInfo(
                $"Opening the unmodified vanilla disconnect confirmation one second {reason}.");
            menu.OnLogout();
        }

        private void ClearPendingRequest()
        {
            _requestId = null;
            _game = null;
            _menu = null;
        }

        private static string Describe(VoluntaryExitKind kind)
        {
            return kind == VoluntaryExitKind.ApplicationQuit
                ? "application quit"
                : kind == VoluntaryExitKind.LogoutPrompt
                    ? "disconnect confirmation"
                    : "logout";
        }
    }

    internal enum VoluntaryExitKind
    {
        Logout,
        LogoutPrompt,
        ApplicationQuit
    }

    internal sealed class VoluntaryExitSaveRequest : IVoluntaryExitSaveRequest
    {
        private readonly Func<bool> _request;

        internal VoluntaryExitSaveRequest(Func<bool> request)
        {
            _request = request;
        }

        public bool Request()
        {
            return _request();
        }
    }
}
