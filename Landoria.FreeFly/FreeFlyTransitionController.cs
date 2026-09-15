using UnityEngine;

namespace Landoria.FreeFly
{
    // Smooths camera movement when entering and leaving free fly.
    internal static class FreeFlyTransitionController
    {
        private const float EnterDuration = 1f; // Seconds.
        private const float ExitDuration = 3f; // Seconds.
        private const float InitialTurnDuration = 1.5f; // Seconds.
        private const float FinalTurnDuration = 1f; // Seconds.

        private static Vector3 startPosition;
        private static Quaternion startRotation;
        private static Vector3 returnPosition;
        private static Quaternion returnRotation;
        private static Vector3 targetPosition;
        private static Quaternion targetRotation;
        private static Vector3 exitLookTarget;
        private static float elapsed;
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
            ApplyExitTransform(camera, Mathf.Min(elapsed, ExitDuration));
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
        private static void ApplyExitTransform(GameCamera camera, float time)
        {
            float progress = time / ExitDuration;
            Vector3 position = Vector3.Lerp(
                startPosition, targetPosition, Ease(progress));
            Quaternion facing = LookAt(position, startRotation);
            float initialTurn = Ease(Mathf.Clamp01(time / InitialTurnDuration));
            Quaternion playerFacing = Quaternion.Slerp(
                startRotation, facing, initialTurn);
            float finalTurnStart = ExitDuration - FinalTurnDuration;
            float finalTurn = Ease(Mathf.Clamp01(
                (time - finalTurnStart) / FinalTurnDuration));
            camera.transform.SetPositionAndRotation(
                position,
                Quaternion.Slerp(playerFacing, targetRotation, finalTurn));
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
