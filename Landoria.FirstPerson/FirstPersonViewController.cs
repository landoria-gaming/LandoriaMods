using UnityEngine;

namespace Landoria.FirstPerson
{
    internal static class FirstPersonViewController
    {
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

            // Keep Valheim's native camera position smoothing.
            // It prevents stuttering during sideways movement.
            camera.transform.rotation = cameraRotation;
        }
    }
}
