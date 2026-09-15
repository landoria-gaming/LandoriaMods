using UnityEngine;

namespace Landoria.FreeFly
{
    // Controls free-fly movement and placement.
    internal static class FreeFlyController
    {
        internal const float DefaultSmoothness = 0.25f;
        internal const float MaximumDistance = 50f;
        internal const float MaximumSpeed = 10f;
        private const float MinimumSpeed = 2f;
        private const float DefaultSpeed = 4f;
        private const float SpeedTransitionSmoothTime = 0.35f; // Seconds.
        internal const float CollisionRadius = 1f;
        internal const float CollisionClearance = 0.05f;
        private const float InitialForwardDistance = 3f;
        private const float InitialHeight = 1f;
        private const float InitialSearchStep = 15f; // Degrees.
        private const int InitialSearchPositions = 24;
        private static bool smoothnessInitialized;
        private static float speedTransitionVelocity;

        // Keeps speed within the allowed range.
        internal static float ClampSpeed(float speed)
        {
            return Mathf.Clamp(speed, MinimumSpeed, MaximumSpeed);
        }

        // Restores the starting speed.
        internal static void SetDefaultSpeed(ref float speed)
        {
            speed = DefaultSpeed;
            speedTransitionVelocity = 0f;
        }

        // Smoothly boosts speed while either Shift key is held.
        internal static void UpdateSpeed(ref float speed, float deltaTime)
        {
            bool boosted = ZInput.GetKey(KeyCode.LeftShift) ||
                           ZInput.GetKey(KeyCode.RightShift);
            float target = boosted ? MaximumSpeed : DefaultSpeed;
            speed = Mathf.SmoothDamp(
                speed, target, ref speedTransitionVelocity,
                SpeedTransitionSmoothTime, Mathf.Infinity, deltaTime);
        }

        // Limits movement speed for the current frame.
        internal static void ClampFrameMovement(GameCamera camera, Vector3 origin, float deltaTime)
        {
            Vector3 movement = camera.transform.position - origin;
            float maximumMovement = MaximumSpeed * deltaTime;
            if (movement.sqrMagnitude > maximumMovement * maximumMovement)
            {
                camera.transform.position = origin + movement.normalized * maximumMovement;
            }
        }

        // Stops the camera before an obstacle.
        internal static void ClampToCollision(GameCamera camera, Vector3 origin)
        {
            Vector3 movement = camera.transform.position - origin;
            float distance = movement.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            if (Physics.SphereCast(origin, CollisionRadius, movement / distance,
                out RaycastHit hit, distance, camera.m_blockCameraMask,
                QueryTriggerInteraction.Ignore))
            {
                float allowedDistance = Mathf.Max(0f, hit.distance - CollisionClearance);
                camera.transform.position = origin + movement.normalized * allowedDistance;
            }
        }

        // Starts or requests the end of free fly.
        internal static void Toggle()
        {
            if (GameCamera.instance == null)
            {
                return;
            }

            if (GameCamera.InFreeFly())
            {
                FreeFlyTransitionController.StartExiting(GameCamera.instance);
            }
            else
            {
                GameCamera.instance.ToggleFreeFly();
            }
        }

        // Prepares the camera for free fly.
        internal static void InitializeActivation(GameCamera camera)
        {
            if (!smoothnessInitialized)
            {
                camera.SetFreeFlySmoothness(DefaultSmoothness);
                smoothnessInitialized = true;
            }
            PositionInFrontOfPlayer(camera);
        }

        // Clears values kept for the current mod load.
        internal static void Reset()
        {
            smoothnessInitialized = false;
            speedTransitionVelocity = 0f;
        }

        // Matches Valheim's angles to the camera direction.
        internal static void SynchronizeRotation(
            Vector3 direction, ref float yaw, ref float pitch)
        {
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) *
                    Mathf.Rad2Deg;
        }

        // Places the camera near and facing the player.
        private static void PositionInFrontOfPlayer(GameCamera camera)
        {
            Player player = Player.m_localPlayer;
            if (!player)
            {
                return;
            }

            Vector3 lookTarget = player.m_eye.position;
            if (TryFindInitialPosition(
                    camera, lookTarget, player.transform.forward,
                    out Vector3 position))
            {
                camera.transform.position = position;
            }

            Vector3 lookDirection = lookTarget - camera.transform.position;
            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                camera.transform.rotation = Quaternion.LookRotation(
                    lookDirection, Vector3.up);
            }
        }

        // Finds a clear starting position around the player.
        private static bool TryFindInitialPosition(
            GameCamera camera, Vector3 origin, Vector3 forward,
            out Vector3 position)
        {
            for (int index = 0; index < InitialSearchPositions; index++)
            {
                float angle = index * InitialSearchStep;
                Vector3 horizontal = Quaternion.AngleAxis(angle, Vector3.up) *
                                     forward * InitialForwardDistance;
                Vector3 candidate = origin + horizontal + Vector3.up * InitialHeight;
                Vector3 movement = candidate - origin;
                if (!Physics.SphereCast(
                        origin, CollisionRadius, movement.normalized,
                        out _, movement.magnitude, camera.m_blockCameraMask,
                        QueryTriggerInteraction.Ignore) &&
                    !Physics.CheckSphere(
                        candidate, CollisionRadius, camera.m_blockCameraMask,
                        QueryTriggerInteraction.Ignore))
                {
                    position = candidate;
                    return true;
                }
            }

            position = camera.transform.position;
            return false;
        }

        // Starts the free-fly exit transition.
        internal static void Disable()
        {
            if (GameCamera.instance && GameCamera.InFreeFly())
            {
                FreeFlyTransitionController.StartExiting(GameCamera.instance);
            }
        }

        // Turns free fly off after its transition.
        internal static void CompleteDisable()
        {
            if (GameCamera.instance && GameCamera.InFreeFly())
            {
                GameCamera.instance.ToggleFreeFly();
            }
        }

        // Keeps the camera close to the player.
        internal static void ClampToPlayer(GameCamera camera)
        {
            Player player = Player.m_localPlayer;
            if (!GameCamera.InFreeFly() || player == null)
            {
                return;
            }

            Vector3 offset = camera.transform.position - player.transform.position;
            if (offset.sqrMagnitude > MaximumDistance * MaximumDistance)
            {
                camera.transform.position = player.transform.position +
                    offset.normalized * MaximumDistance;
            }
        }
    }
}
