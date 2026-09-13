using HarmonyLib;
using UnityEngine;

namespace Landoria.FreeFlyCommand
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class FreeFlyAuthorizationOnSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                FreeFlyAuthorization.RequestOnSpawn();
            }
        }
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class FreeFlyCommandRegistrationPatch
    {
        private static void Postfix()
        {
            FreeFlyCommands.Register();
        }
    }

    [HarmonyPatch(typeof(Terminal.ConsoleCommand), nameof(Terminal.ConsoleCommand.IsValid))]
    internal static class FreeFlyCommandValidationPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Terminal.ConsoleCommand __instance, ref bool __result)
        {
            if (FreeFlyCommands.IsManaged(__instance) && !FreeFlyAuthorization.IsAuthorized)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.ToggleFreeFly))]
    internal static class UnauthorizedFreeFlyTogglePatch
    {
        private static bool Prefix()
        {
            return FreeFlyAuthorization.IsAuthorized || GameCamera.InFreeFly();
        }
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateFreeFly")]
    internal static class FreeFlyMovementPatch
    {
        private static void Prefix(
            GameCamera __instance, ref float ___m_freeFlySpeed, out Vector3 __state)
        {
            ___m_freeFlySpeed = FreeFlyController.ClampSpeed(___m_freeFlySpeed);
            __state = __instance.transform.position;
        }

        private static void Postfix(
            GameCamera __instance, float dt, ref float ___m_freeFlySpeed, Vector3 __state)
        {
            ___m_freeFlySpeed = FreeFlyController.ClampSpeed(___m_freeFlySpeed);
            FreeFlyController.ClampFrameMovement(__instance, __state, dt);
            FreeFlyController.ClampToCollision(__instance, __state);
            FreeFlyController.ClampToPlayer(__instance);
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class FreeFlyDisconnectPatch
    {
        private static void Prefix()
        {
            FreeFlyAuthorization.ResetSession();
        }
    }
}
