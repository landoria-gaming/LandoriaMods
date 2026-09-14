using UnityEngine;

namespace Landoria.FirstPerson
{
    // Adds a small camera impulse synchronized with the local player's footsteps.
    internal static class FirstPersonHeadBobController
    {
        private const float WalkDuration = 0.22f;
        private const float RunDuration = 0.16f;
        private const float MetersPerMillimeter = 0.001f;

        private static float elapsed = float.PositiveInfinity;
        private static float duration;
        private static float verticalAmplitude;
        private static float horizontalDirection = 1f;

        // Starts one bob impulse for a vanilla walking or running step.
        internal static void Trigger(FootStep.MotionType motionType)
        {
            if (!FirstPersonPlugin.HeadBobEnabled || !FirstPersonMode.Active ||
                !IsGroundStep(motionType))
            {
                return;
            }

            bool running = (motionType & FootStep.MotionType.Run) != 0;
            duration = running ? RunDuration : WalkDuration;
            verticalAmplitude = (running
                ? FirstPersonPlugin.HeadBobRunVerticalAmplitude
                : FirstPersonPlugin.HeadBobWalkVerticalAmplitude) *
                MetersPerMillimeter;
            horizontalDirection = -horizontalDirection;
            elapsed = 0f;
        }

        // Applies the current impulse after Valheim has positioned the camera.
        internal static void Apply(GameCamera camera)
        {
            if (!FirstPersonPlugin.HeadBobEnabled || !FirstPersonMode.Active ||
                !camera || elapsed >= duration)
            {
                return;
            }

            elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
            float progress = elapsed / duration;
            float contact = Mathf.Sin(progress * Mathf.PI);
            float rebound = Mathf.Sin(progress * Mathf.PI * 2f) * 0.15f;
            float vertical = (-contact + rebound) * verticalAmplitude;
            float horizontal = contact * FirstPersonPlugin.HeadBobHorizontalAmplitude *
                               MetersPerMillimeter * horizontalDirection;
            camera.transform.position += camera.transform.up * vertical +
                                         camera.transform.right * horizontal;
        }

        internal static void Reset()
        {
            elapsed = float.PositiveInfinity;
        }

        private static bool IsGroundStep(FootStep.MotionType motionType)
        {
            const FootStep.MotionType groundSteps = FootStep.MotionType.Jog |
                                                    FootStep.MotionType.Run |
                                                    FootStep.MotionType.Sneak |
                                                    FootStep.MotionType.Walk;
            return (motionType & groundSteps) != 0;
        }
    }
}
