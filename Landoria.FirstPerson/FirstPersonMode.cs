namespace Landoria.FirstPerson
{
    // Stores and applies the current first-person camera state.
    internal static class FirstPersonMode
    {
        private const float NearClipPlane = 0.09f; // Meters.
        private const float DistanceThreshold = 0.001f; // Meters.

        private static float vanillaMinimumDistance;
        private static bool distanceCaptured;

        internal static bool Enabled { get; private set; }
        internal static bool Active { get; private set; }

        // Remembers Valheim's original minimum camera distance.
        internal static void CaptureVanillaDistance(GameCamera camera)
        {
            vanillaMinimumDistance = camera.m_minDistance;
            distanceCaptured = true;
        }

        // Allows zero camera distance only when first person is enabled.
        internal static void Apply(GameCamera camera)
        {
            if (!camera)
            {
                return;
            }

            camera.m_minDistance = Enabled ? 0f : vanillaMinimumDistance;
        }

        // Enables or disables first-person mode.
        internal static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (!enabled)
            {
                SetActive(false);
                FirstPersonShortcut.CancelTransition();
            }
            Apply(GameCamera.instance);
            ApplyConfiguredFieldOfView(GameCamera.instance);
        }

        // Records whether the camera is currently in first person.
        internal static void SetActive(bool active)
        {
            if (Active == active)
            {
                return;
            }

            Active = active;
            if (active)
            {
                FirstPersonVegetationController.Apply();
            }
            else
            {
                FirstPersonVegetationController.Restore();
            }
        }

        // Checks whether first person should be active for this frame.
        internal static bool ShouldActivate(
            Player player, bool isFreeFly, float cameraDistance)
        {
            return Enabled && player && !player.IsDead() && !isFreeFly &&
                   IsFirstPersonDistance(cameraDistance);
        }

        internal static bool IsFirstPersonDistance(float cameraDistance)
        {
            return cameraDistance <= DistanceThreshold;
        }

        internal static float GetMinimumThirdPersonDistance()
        {
            return vanillaMinimumDistance;
        }

        // Applies a field of view to the active game camera.
        internal static void SetFieldOfView(GameCamera camera, float fieldOfView)
        {
            if (camera)
            {
                camera.m_fov = fieldOfView;
            }
        }

        // Applies the saved field of view to every camera mode.
        internal static void ApplyConfiguredFieldOfView(GameCamera camera)
        {
            SetFieldOfView(camera, FirstPersonPreference.FieldOfView);
        }

        // Reduces nearby geometry clipping while first person is active.
        internal static void ApplyNearClipPlane(UnityEngine.Camera camera)
        {
            if (Active && camera)
            {
                camera.nearClipPlane = NearClipPlane;
            }
        }

        // Disables first person when leaving the current game session.
        internal static void ResetSession()
        {
            SetEnabled(false);
            FirstPersonHeadBobController.Reset();
            FirstPersonHelmetLightController.Restore();
            FirstPersonVisibilityController.Restore();
            FirstPersonShortcut.Reset();
        }

        // Restores all camera and visual state when the plugin stops.
        internal static void Reset()
        {
            ResetSession();
            if (distanceCaptured && GameCamera.instance)
            {
                GameCamera.instance.m_minDistance = vanillaMinimumDistance;
            }

            distanceCaptured = false;
        }
    }
}
