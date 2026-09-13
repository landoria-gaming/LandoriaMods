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
        private static void Postfix(GameCamera __instance, float ___m_distance)
        {
            Player player = Player.m_localPlayer;
            bool shouldApply = FirstPersonMode.ShouldActivate(
                player, GameCamera.InFreeFly(), ___m_distance);
            FirstPersonMode.SetActive(shouldApply);
            FirstPersonMode.ApplyConfiguredFieldOfView(__instance);
            FirstPersonVisibilityController.SetHidden(player, shouldApply);
            if (shouldApply)
            {
                FirstPersonViewController.Apply(__instance, player);
                FirstPersonHelmetLightController.Apply(__instance, player);
            }
            else
            {
                FirstPersonHelmetLightController.Restore();
            }
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

    // Registers the first-person command whenever the terminal is created.
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class FirstPersonCommandRegistrationPatch
    {
        private static void Postfix()
        {
            FirstPersonCommand.Register();
        }
    }

    // Validates and saves values handled by Valheim's FOV command.
    [HarmonyPatch(typeof(Terminal.ConsoleCommand), "RunAction")]
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
                bool exceedsMaximum = __instance.Command == "fov" && args.Length > 1 &&
                                      parsed && requestedFieldOfView >
                                      FirstPersonPreference.MaximumFieldOfView;
                if (!exceedsMaximum)
                {
                    return true;
                }

                args.Context?.AddString(
                    $"FOV must not exceed {FirstPersonPreference.MaximumFieldOfView}. " +
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
                              parsed && fieldOfView > 5f && fieldOfView <=
                              FirstPersonPreference.MaximumFieldOfView;
            if (shouldSave)
            {
                FirstPersonPreference.SetFieldOfView(fieldOfView);
                FirstPersonMode.ApplyConfiguredFieldOfView(GameCamera.instance);
            }
        }
    }

    // Restores saved first-person settings when the local player spawns.
    [HarmonyPatch(typeof(Player), "OnSpawned")]
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
            FirstPersonHelmetLightController.Restore();
            FirstPersonVisibilityController.Restore();
            FirstPersonMode.ResetSession();
        }
    }
}
