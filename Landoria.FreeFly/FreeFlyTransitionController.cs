using UnityEngine;

namespace Landoria.FreeFly
{
    // Smooths camera movement when entering and leaving free fly.
    internal static class FreeFlyTransitionController
    {
        private const float EnterDuration = 1f; // Seconds.
        private const float ExitDuration = 2f; // Seconds.
        private const float TurnEnd = 0.35f;
        private const float ApproachEnd = 0.85f;
        private const float FinalTurnStart = 0.7f;

        private static Vector3 startPosition;
        private static Quaternion startRotation;
        private static Vector3 returnPosition;
        private static Quaternion returnRotation;
        private static Vector3 targetPosition;
        private static Quaternion targetRotation;
        private static Vector3 exitLookTarget;
        private static float elapsed;
        private static float approachStart;
        private static bool entering;
        private static bool exiting;

        // Starts the transition into free fly.
        internal static void StartEntering(
            GameCamera camera, Vector3 position, Quaternion rotation)
        {
            startPosition = position;
            startRotation = rotation;
            returnPosition = position;
            returnRotation = rotation;
            targetPosition = camera.transform.position;
            targetRotation = camera.transform.rotation;
            elapsed = 0f;
            entering = true;
            exiting = false;
            camera.transform.SetPositionAndRotation(position, rotation);
        }

        // Starts the transition back to the saved camera.
        internal static void StartExiting(GameCamera camera)
        {
            if (exiting)
            {
                return;
            }

            startPosition = camera.transform.position;
            startRotation = camera.transform.rotation;
            targetPosition = returnPosition;
            targetRotation = returnRotation;
            Player player = Player.m_localPlayer;
            exitLookTarget = player
                ? player.m_eye.position
                : camera.transform.position + camera.transform.forward;
            elapsed = 0f;
            approachStart = -1f;
            entering = false;
            exiting = true;
        }

        // Advances the entry transition.
        internal static void ApplyEntering(GameCamera camera, float deltaTime)
        {
            if (!entering)
            {
                return;
            }

            ApplyEnteringTransform(camera, deltaTime);
            entering = elapsed < EnterDuration;
        }

        // Advances the exit transition.
        internal static void ApplyExiting(GameCamera camera, float deltaTime)
        {
            if (!exiting)
            {
                return;
            }

            elapsed += deltaTime;
            ApplyExitTransform(camera, Mathf.Clamp01(elapsed / ExitDuration));
            exiting = elapsed < ExitDuration;
            if (!exiting)
            {
                FreeFlyController.CompleteDisable();
            }
        }

        // Blends toward the free-fly starting pose.
        private static void ApplyEnteringTransform(
            GameCamera camera, float deltaTime)
        {
            elapsed += deltaTime;
            float eased = Ease(Mathf.Clamp01(elapsed / EnterDuration));
            camera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, eased),
                Quaternion.Slerp(startRotation, targetRotation, eased));
        }

        // Blends the three exit movements together.
        private static void ApplyExitTransform(GameCamera camera, float progress)
        {
            float initialTurn = Ease(Mathf.Clamp01(progress / TurnEnd));
            Quaternion facingStart = LookAt(startPosition, startRotation);
            Quaternion playerFacing = Quaternion.Slerp(
                startRotation, facingStart, initialTurn);
            if (approachStart < 0f && IsPlayerInView(camera, playerFacing))
            {
                approachStart = progress;
            }

            float approach = GetApproachProgress(progress);
            Vector3 position = Vector3.Lerp(
                startPosition, targetPosition, Ease(approach));
            Quaternion facing = LookAt(position, playerFacing);
            Quaternion approachFacing = Quaternion.Slerp(
                playerFacing, facing, Ease(approach));
            float finalTurn = Mathf.Clamp01(
                (progress - FinalTurnStart) / (1f - FinalTurnStart));
            camera.transform.SetPositionAndRotation(
                position,
                Quaternion.Slerp(approachFacing, targetRotation, Ease(finalTurn)));
        }

        // Starts and advances the approach after the player becomes visible.
        private static float GetApproachProgress(float progress)
        {
            if (approachStart < 0f)
            {
                return 0f;
            }

            float duration = Mathf.Max(0.01f, ApproachEnd - approachStart);
            return Mathf.Clamp01((progress - approachStart) / duration);
        }

        // Checks whether the player is inside the camera view angle.
        private static bool IsPlayerInView(GameCamera camera, Quaternion rotation)
        {
            Vector3 direction = (exitLookTarget - startPosition).normalized;
            float angle = Vector3.Angle(rotation * Vector3.forward, direction);
            return angle <= camera.m_fov * 0.5f;
        }

        // Returns a rotation facing the player.
        private static Quaternion LookAt(Vector3 position, Quaternion fallback)
        {
            Vector3 direction = exitLookTarget - position;
            return direction.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(direction, Vector3.up)
                : fallback;
        }

        // Smooths acceleration and deceleration.
        private static float Ease(float progress)
        {
            return progress * progress * progress *
                   (progress * (progress * 6f - 15f) + 10f);
        }
    }
}
