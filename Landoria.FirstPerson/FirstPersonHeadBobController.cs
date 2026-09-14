using UnityEngine;

namespace Landoria.FirstPerson
{
    // Moves the camera through alternating halves of a continuous figure eight.
    internal static class FirstPersonHeadBobController
    {
        private const float MetersPerMillimeter = 0.001f; // Meters per millimeter.
        private const float MovementThreshold = 0.01f; // Squared unitless input.
        private const float FadeDuration = 0.12f; // Seconds.

        private static float phase;
        private static float blend;

        // Applies a continuous cycle after Valheim has positioned the camera.
        internal static void Apply(GameCamera camera, Player player)
        {
            if (FirstPersonPlugin.HeadBobMultiplier <= 0f || !FirstPersonMode.Active ||
                !camera || !player)
            {
                return;
            }

            bool moving = player.IsOnGround() &&
                          player.GetMoveDir().sqrMagnitude > MovementThreshold;
            blend = Mathf.MoveTowards(
                blend, moving ? 1f : 0f, Time.deltaTime / FadeDuration);
            bool running = player.IsRunning();
            float halfCycleDuration = running
                ? FirstPersonPlugin.HeadBobSprintStepInterval
                : player.IsWalking()
                    ? FirstPersonPlugin.HeadBobWalkStepInterval
                    : FirstPersonPlugin.HeadBobJogStepInterval;
            phase = Mathf.Repeat(
                phase + Time.deltaTime * Mathf.PI / halfCycleDuration,
                Mathf.PI * 2f);
            float verticalAmplitude = (running
                ? FirstPersonPlugin.HeadBobSprintVerticalAmplitude
                : player.IsWalking()
                    ? FirstPersonPlugin.HeadBobWalkVerticalAmplitude
                    : FirstPersonPlugin.HeadBobJogVerticalAmplitude) *
                MetersPerMillimeter * FirstPersonPlugin.HeadBobMultiplier;
            float horizontalAmplitude = FirstPersonPlugin.HeadBobHorizontalAmplitude *
                                        MetersPerMillimeter *
                                        FirstPersonPlugin.HeadBobMultiplier;
            float horizontal = Mathf.Sin(phase) * horizontalAmplitude * blend;
            float vertical = Mathf.Sin(phase * 2f) * verticalAmplitude * blend;
            ApplyMovement(camera.transform, horizontal, vertical);
        }

        private static void ApplyMovement(
            Transform transform, float horizontal, float vertical)
        {
            Vector3 horizonPoint = transform.position + transform.forward *
                                   FirstPersonPlugin.HeadBobHorizonDistance;
            transform.position += transform.right * horizontal +
                                  transform.up * vertical;
            transform.rotation = Quaternion.LookRotation(
                horizonPoint - transform.position, Vector3.up);
        }

        internal static void Reset()
        {
            phase = 0f;
            blend = 0f;
        }
    }
}
