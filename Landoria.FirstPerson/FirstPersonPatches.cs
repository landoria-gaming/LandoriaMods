using HarmonyLib;
using UnityEngine;

namespace Landoria.FirstPerson
{
    // Saves and applies camera distance settings when the camera starts.
    [HarmonyPatch(typeof(GameCamera), "Awake")]
    internal static class FirstPersonCameraAwakePatch
    {
        private static void Prefix(GameCamera __instance)
        {
            FirstPersonMode.CaptureVanillaDistance(__instance);
        }

        private static void Postfix(GameCamera __instance)
        {
            FirstPersonMode.Apply(__instance);
        }
    }

    // Updates first-person state after Valheim positions the camera.
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    internal static class FirstPersonCameraUpdatePatch
    {
        private static void Prefix(
            GameCamera __instance, float dt, ref float ___m_distance)
        {
            FirstPersonShortcut.Update(__instance, dt, ref ___m_distance);
            FirstPersonShortcut.PrepareDistanceObservation(___m_distance);
        }

        private static void Postfix(
            GameCamera __instance, Camera ___m_camera, float ___m_distance)
        {
            Player player = Player.m_localPlayer;
            bool shouldApply = FirstPersonMode.ShouldActivate(
                player, GameCamera.InFreeFly(), ___m_distance);
            FirstPersonMode.SetActive(shouldApply);
            FirstPersonMode.ApplyConfiguredFieldOfView(__instance);
            FirstPersonMode.ApplyNearClipPlane(___m_camera);
            FirstPersonVisibilityController.SetHidden(player, shouldApply);
            float offsetWeight = FirstPersonShortcut.GetOffsetWeight(shouldApply);
            if (offsetWeight > 0f)
            {
                FirstPersonViewController.Apply(
                    __instance, player, offsetWeight, shouldApply);
            }
            if (shouldApply)
            {
                FirstPersonHeadBobController.Apply(__instance, player);
                FirstPersonHelmetLightController.Apply(__instance, player);
            }
            else
            {
                FirstPersonHeadBobController.Reset();
                FirstPersonHelmetLightController.Restore();
            }
        }
    }

    // Handles native zoom before Valheim calculates the final camera position.
    [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
    internal static class FirstPersonTemporaryDistancePatch
    {
        private static void Prefix(GameCamera __instance, ref float ___m_distance)
        {
            FirstPersonShortcut.ObserveCameraDistance(
                __instance, ref ___m_distance);
            FirstPersonShortcut.KeepTemporaryThirdPerson(ref ___m_distance);
        }
    }

    // Blends Valheim's third-person camera offset into its first-person offset.
    [HarmonyPatch(typeof(GameCamera), "GetCameraOffset")]
    internal static class FirstPersonCameraOffsetPatch
    {
        private static void Postfix(
            GameCamera __instance, Player player, ref Vector3 __result)
        {
            float weight = FirstPersonShortcut.GetOffsetWeight(FirstPersonMode.Active);
            if (weight <= 0f || !player) return;

            Vector3 firstPersonOffset = player.m_eye.transform.TransformVector(
                __instance.m_fpsOffset);
            __result = Vector3.Lerp(__result, firstPersonOffset, weight);
        }
    }

    // Keeps movement relative to the camera without turning the body toward strafing.
    [HarmonyPatch(typeof(Player), "AlwaysRotateCamera")]
    internal static class FirstPersonPlayerRotationPatch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (FirstPersonMode.Active && __instance == Player.m_localPlayer)
            {
                __result = true;
            }
        }
    }

    // Updates vegetation materials on objects loaded after first person activates.
    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class FirstPersonLoadedObjectPatch
    {
        private static void Postfix(ZNetView __instance)
        {
            FirstPersonVegetationController.Apply(__instance.gameObject);
        }
    }

    // Keeps the local character active so its animations still run while hidden.
    [HarmonyPatch(typeof(Character), "SetVisible")]
    internal static class FirstPersonPlayerVisibilityPatch
    {
        private static void Prefix(Character __instance, ref bool visible)
        {
            if (FirstPersonMode.Active && __instance == Player.m_localPlayer)
            {
                visible = true;
            }
        }
    }

    // Refreshes hidden visuals and held items after equipment changes.
    [HarmonyPatch(typeof(VisEquipment), "UpdateVisuals")]
    internal static class FirstPersonVisualVisibilityPatch
    {
        private static void Postfix(
            VisEquipment __instance, GameObject ___m_leftItemInstance,
            GameObject ___m_rightItemInstance)
        {
            Player player = __instance.GetComponentInParent<Player>();
            if (player == Player.m_localPlayer)
            {
                FirstPersonVisibilityController.TrackHeldItems(
                    player, ___m_leftItemInstance, ___m_rightItemInstance);
            }
            if (FirstPersonMode.Active && player == Player.m_localPlayer)
            {
                FirstPersonHelmetLightController.Refresh(player);
            }
        }
    }

    // Repositions helmet lights after Valheim finishes its frame updates.
    [HarmonyPatch(typeof(MonoUpdaters), "LateUpdate")]
    internal static class FirstPersonHelmetLightLateUpdatePatch
    {
        private static void Postfix()
        {
            FirstPersonHelmetLightController.Apply(
                GameCamera.instance, Player.m_localPlayer);
        }
    }

    // Validates and saves values handled by Valheim's FOV command.
    [HarmonyPatch(typeof(Terminal.ConsoleCommand), nameof(Terminal.ConsoleCommand.RunAction))]
    internal static class FirstPersonFieldOfViewCommandPatch
    {
        private static bool Prefix(
            Terminal.ConsoleCommand __instance, Terminal.ConsoleEventArgs args)
        {
            string value = args.Length > 1 ? args[1] : null;
            bool shouldReset = __instance.Command == "fov" && args.Length == 2 &&
                               string.Equals(value, "reset",
                                   System.StringComparison.OrdinalIgnoreCase);
            if (!shouldReset)
            {
                bool parsed = args.TryParameterFloat(1, out float requestedFieldOfView);
                bool outsideSupportedRange = __instance.Command == "fov" &&
                                             args.Length > 1 && parsed &&
                                             (requestedFieldOfView <
                                              FirstPersonPreference.MinimumFieldOfView ||
                                              requestedFieldOfView >
                                              FirstPersonPreference.MaximumFieldOfView);
                if (!outsideSupportedRange)
                {
                    return true;
                }

                args.Context?.AddString(
                    $"FOV must be between {FirstPersonPreference.MinimumFieldOfView} " +
                    $"and {FirstPersonPreference.MaximumFieldOfView}. " +
                    "The current FOV was not changed.");
                return false;
            }

            float fieldOfView = FirstPersonPreference.DefaultFieldOfView;
            FirstPersonPreference.SetFieldOfView(fieldOfView);
            FirstPersonMode.ApplyConfiguredFieldOfView(GameCamera.instance);
            return false;
        }

        private static void Postfix(
            Terminal.ConsoleCommand __instance, Terminal.ConsoleEventArgs args)
        {
            bool parsed = args.TryParameterFloat(1, out float fieldOfView);
            bool shouldSave = __instance.Command == "fov" && args.Length > 1 &&
                              parsed && fieldOfView >=
                              FirstPersonPreference.MinimumFieldOfView &&
                              fieldOfView <=
                              FirstPersonPreference.MaximumFieldOfView;
            if (shouldSave)
            {
                FirstPersonPreference.SetFieldOfView(fieldOfView);
                FirstPersonMode.ApplyConfiguredFieldOfView(GameCamera.instance);
            }
        }
    }

    // Restores saved first-person settings when the local player spawns.
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class FirstPersonPlayerSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                FirstPersonMode.SetEnabled(FirstPersonPreference.Enabled);
                FirstPersonMode.ApplyConfiguredFieldOfView(GameCamera.instance);
            }
        }
    }

    // Restores changed state when the player leaves a game session.
    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class FirstPersonDisconnectPatch
    {
        private static void Prefix()
        {
            FirstPersonMode.ResetSession();
        }
    }
}
