using BepInEx.Configuration;
using UnityEngine;

namespace Landoria.FirstPerson
{
    // Toggles first person while preserving the previous camera distance.
    internal static class FirstPersonShortcut
    {
        private const float TransitionDuration = 0.2f;

        private static float previousDistance;
        private static bool hasPreviousDistance;
        private static bool temporarilyThirdPerson;
        private static float firstPersonReturnTime;
        private static bool transitioning;
        private static float transitionStartDistance;
        private static float transitionTargetDistance;
        private static float transitionElapsed;
        private static float transitionStartOffsetWeight;
        private static float transitionTargetOffsetWeight;
        private static float transitionOffsetWeight;
        private static float expectedCameraDistance;
        private static bool awaitingDistanceObservation;

        internal static void Update(
            GameCamera camera, float deltaTime, ref float cameraDistance)
        {
            ApplyTransition(deltaTime, ref cameraDistance);
            RememberDistance(cameraDistance);
            if (!CanToggle()) return;

            if (IsToggleShortcutDown())
            {
                ToggleShortcut(camera, ref cameraDistance);
                return;
            }

            UpdateActionReturn(camera, ref cameraDistance);
        }

        private static void RememberDistance(float cameraDistance)
        {
            if (!transitioning &&
                !FirstPersonMode.IsFirstPersonDistance(cameraDistance))
            {
                previousDistance = cameraDistance;
                hasPreviousDistance = true;
            }
        }

        private static void ToggleShortcut(GameCamera camera, ref float cameraDistance)
        {
            temporarilyThirdPerson = false;
            firstPersonReturnTime = 0f;
            bool enabled = !FirstPersonMode.Enabled;
            FirstPersonMode.SetEnabled(enabled);
            FirstPersonPreference.SetEnabled(enabled);
            float targetDistance = enabled
                ? 0f
                : hasPreviousDistance ? previousDistance : camera.m_maxDistance;
            StartTransition(ref cameraDistance, targetDistance);
            ShowState(enabled);
        }

        private static void UpdateActionReturn(GameCamera camera, ref float cameraDistance)
        {
            float returnDelay = Mathf.Max(
                0f, FirstPersonPreference.CombatReturnDelay);
            bool actionActive = returnDelay > 0f && IsCombatActionActive();
            bool isFirstPerson = FirstPersonMode.Enabled &&
                                 (FirstPersonMode.IsFirstPersonDistance(cameraDistance) ||
                                  IsTransitioningToFirstPerson());
            if (actionActive && (temporarilyThirdPerson || isFirstPerson))
            {
                if (!temporarilyThirdPerson)
                {
                    StartTransition(ref cameraDistance,
                        hasPreviousDistance ? previousDistance : camera.m_maxDistance);
                }
                temporarilyThirdPerson = true;
                firstPersonReturnTime = Mathf.Max(
                    firstPersonReturnTime,
                    Time.unscaledTime + returnDelay);
            }
            else if (temporarilyThirdPerson && Time.unscaledTime >= firstPersonReturnTime)
            {
                StartTransition(ref cameraDistance, 0f);
                temporarilyThirdPerson = false;
            }
            else if (!temporarilyThirdPerson && FirstPersonMode.Enabled &&
                     !transitioning &&
                     !FirstPersonMode.IsFirstPersonDistance(cameraDistance))
            {
                StartTransition(ref cameraDistance, 0f);
            }
        }

        internal static void PrepareDistanceObservation(float cameraDistance)
        {
            expectedCameraDistance = cameraDistance;
            awaitingDistanceObservation = true;
        }

        internal static void ObserveCameraDistance(
            GameCamera camera, ref float cameraDistance)
        {
            bool changed = awaitingDistanceObservation && FirstPersonMode.Enabled &&
                           Mathf.Abs(cameraDistance - expectedCameraDistance) >
                           Mathf.Epsilon;
            awaitingDistanceObservation = false;
            if (!changed) return;

            float returnDelay = Mathf.Max(
                0f, FirstPersonPreference.ZoomReturnDelay);
            if (returnDelay <= 0f && !temporarilyThirdPerson)
            {
                cameraDistance = expectedCameraDistance;
                return;
            }

            float maximumDistance = Player.m_localPlayer.GetControlledShip() != null
                ? camera.m_maxDistanceBoat
                : camera.m_maxDistance;
            cameraDistance = Mathf.Clamp(
                cameraDistance,
                FirstPersonMode.GetMinimumThirdPersonDistance(),
                maximumDistance);
            previousDistance = cameraDistance;
            hasPreviousDistance = true;
            transitioning = false;
            transitionOffsetWeight = 0f;
            if (returnDelay > 0f)
            {
                temporarilyThirdPerson = true;
                firstPersonReturnTime = Mathf.Max(
                    firstPersonReturnTime, Time.unscaledTime + returnDelay);
            }
        }

        internal static void KeepTemporaryThirdPerson(ref float cameraDistance)
        {
            if ((temporarilyThirdPerson || IsTransitioningToFirstPerson()) &&
                FirstPersonMode.IsFirstPersonDistance(cameraDistance))
            {
                cameraDistance = FirstPersonMode.GetMinimumThirdPersonDistance();
            }
        }

        private static bool IsCombatActionActive()
        {
            Player player = Player.m_localPlayer;
            return player && (player.InAttack() || player.IsBlocking());
        }

        private static void StartTransition(ref float cameraDistance, float targetDistance)
        {
            transitionStartDistance = cameraDistance;
            transitionTargetDistance = targetDistance;
            transitionStartOffsetWeight = transitioning
                ? transitionOffsetWeight
                : FirstPersonMode.IsFirstPersonDistance(cameraDistance) ? 1f : 0f;
            transitionTargetOffsetWeight = FirstPersonMode.IsFirstPersonDistance(targetDistance)
                ? 1f
                : 0f;
            transitionElapsed = 0f;
            transitioning = true;
        }

        private static void ApplyTransition(float deltaTime, ref float cameraDistance)
        {
            if (!transitioning) return;

            transitionElapsed += deltaTime;
            float progress = Mathf.Clamp01(transitionElapsed / TransitionDuration);
            float easedProgress = EaseInOut(progress);
            cameraDistance = Mathf.Lerp(
                transitionStartDistance, transitionTargetDistance, easedProgress);
            transitionOffsetWeight = Mathf.Lerp(
                transitionStartOffsetWeight, transitionTargetOffsetWeight, easedProgress);
            transitioning = progress < 1f;
        }

        private static float EaseInOut(float progress)
        {
            return progress * progress * progress *
                   (progress * (progress * 6f - 15f) + 10f);
        }

        internal static float GetOffsetWeight(bool firstPersonActive)
        {
            return transitioning
                ? transitionOffsetWeight
                : firstPersonActive ? 1f : 0f;
        }

        private static bool IsTransitioningToFirstPerson()
        {
            return transitioning &&
                   FirstPersonMode.IsFirstPersonDistance(transitionTargetDistance);
        }

        internal static void CancelTransition()
        {
            transitioning = false;
            temporarilyThirdPerson = false;
            transitionOffsetWeight = 0f;
            expectedCameraDistance = 0f;
            awaitingDistanceObservation = false;
        }

        internal static void Reset()
        {
            previousDistance = 0f;
            hasPreviousDistance = false;
            temporarilyThirdPerson = false;
            firstPersonReturnTime = 0f;
            transitioning = false;
            transitionStartDistance = 0f;
            transitionTargetDistance = 0f;
            transitionElapsed = 0f;
            transitionStartOffsetWeight = 0f;
            transitionTargetOffsetWeight = 0f;
            transitionOffsetWeight = 0f;
        }

        private static bool CanToggle()
        {
            Player player = Player.m_localPlayer;
            return player && !player.IsDead() && !player.InCutscene() &&
                   !GameCamera.InFreeFly() && !Console.IsVisible() &&
                   !Menu.IsVisible() && !InventoryGui.IsVisible() &&
                   !StoreGui.IsVisible() && !Minimap.IsOpen() &&
                   !Hud.IsPieceSelectionVisible() && !Hud.InRadial() &&
                   (Chat.instance == null || !Chat.instance.HasFocus());
        }

        private static bool IsToggleShortcutDown()
        {
            KeyboardShortcut shortcut = FirstPersonPreference.ToggleShortcut;
            if (shortcut.MainKey == KeyCode.None ||
                !ZInput.GetKeyDown(shortcut.MainKey))
            {
                return false;
            }

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!ZInput.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ShowState(bool enabled)
        {
            Player.m_localPlayer?.Message(
                MessageHud.MessageType.TopLeft,
                enabled ? "First Person" : "Third Person");
        }
    }
}
