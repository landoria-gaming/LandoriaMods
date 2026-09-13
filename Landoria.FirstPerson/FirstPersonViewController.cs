using UnityEngine;

namespace Landoria.FirstPerson
{
    // Controls the local player's view while first person is active.
    internal static class FirstPersonViewController
    {
        // Aligns the player with the camera while keeping Valheim's camera position.
        internal static void Apply(GameCamera camera, Player player)
        {
            if (!camera || !player || player.IsAttached() || player.InCutscene())
            {
                return;
            }

            Quaternion cameraRotation = camera.transform.rotation;
            Vector3 lookDirection = camera.transform.forward;
            Vector3 bodyDirection = Vector3.ProjectOnPlane(lookDirection, Vector3.up);
            if (bodyDirection.sqrMagnitude > Mathf.Epsilon)
            {
                player.SetLookDir(lookDirection);
                player.transform.rotation = Quaternion.LookRotation(bodyDirection, Vector3.up);
            }

            // Keep the position produced by GameCamera so its native player-motion
            // smoothing is not replaced with the animated eye transform.
            camera.transform.rotation = cameraRotation;
        }
    }
}
