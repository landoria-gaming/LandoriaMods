using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Landoria.GetMyTrophyBack
{
    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.GetHoverText))]
    internal static class ShowInteractionForActivePowerPatch
    {
        private static void Postfix(ItemStand __instance, ref string __result)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_guardianPower == null ||
                !__instance.HaveAttachment() || __instance.m_canBeRemoved ||
                __instance.IsInvoking("DelayedPowerActivation") ||
                player.GetGuardianPowerName() != __instance.m_guardianPower.name ||
                !PrivateArea.CheckAccess(__instance.transform.position, 0f, false))
            {
                return;
            }

            DropTrophyForActivePowerPatch.EnsureInteractionScheduled(__instance);
            if (!DropTrophyForActivePowerPatch.IsReady(__instance))
            {
                return;
            }

            string tooltip = __instance.m_guardianPower.GetTooltipString();
            __result = Localization.instance.Localize(
                "<color=orange>" + __instance.m_guardianPower.m_name + "</color>\n" +
                tooltip + "\n\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact))]
    internal static class DropTrophyForActivePowerPatch
    {
        private const float InteractionDelaySeconds = 15f;
        private static readonly HashSet<ItemStand> Waiting = new HashSet<ItemStand>();
        private static readonly HashSet<ItemStand> Ready = new HashSet<ItemStand>();

        internal static bool IsReady(ItemStand itemStand)
        {
            return Ready.Contains(itemStand);
        }

        internal static void EnsureInteractionScheduled(ItemStand itemStand)
        {
            if (Ready.Contains(itemStand) || !Waiting.Add(itemStand))
            {
                return;
            }

            itemStand.StartCoroutine(EnableInteractionAfterDelay(itemStand));
            GetMyTrophyBackPlugin.Log.LogDebug(
                $"Scheduled trophy interaction for active power {itemStand.m_guardianPower.name}.");
        }

        private static bool Prefix(ItemStand __instance, Humanoid user, bool hold,
            ref bool __result)
        {
            if (hold || __instance.m_guardianPower == null ||
                !__instance.HaveAttachment() || !(user is Player player) ||
                player != Player.m_localPlayer ||
                player.GetGuardianPowerName() != __instance.m_guardianPower.name)
            {
                return true;
            }

            if (!Ready.Remove(__instance))
            {
                EnsureInteractionScheduled(__instance);
                __result = true;
                return false;
            }

            TrophyDropService.RequestDrop(__instance);
            GetMyTrophyBackPlugin.Log.LogDebug(
                $"Requested immediate trophy drop for active power {__instance.m_guardianPower.name}.");
            __result = true;
            return false;
        }

        private static IEnumerator EnableInteractionAfterDelay(ItemStand itemStand)
        {
            yield return new WaitForSeconds(InteractionDelaySeconds);
            Waiting.Remove(itemStand);

            Player player = Player.m_localPlayer;
            if (itemStand == null || player == null || itemStand.m_guardianPower == null ||
                !itemStand.HaveAttachment() ||
                player.GetGuardianPowerName() != itemStand.m_guardianPower.name)
            {
                Ready.Remove(itemStand);
                yield break;
            }

            Ready.Add(itemStand);
            GetMyTrophyBackPlugin.Log.LogDebug(
                $"Enabled trophy interaction for active power {itemStand.m_guardianPower.name}.");
        }
    }

    [HarmonyPatch(typeof(ItemStand), "DelayedPowerActivation")]
    internal static class DropTrophyAfterPowerActivationPatch
    {
        private static void Postfix(ItemStand __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_guardianPower == null)
            {
                return;
            }

            if (player.GetGuardianPowerName() != __instance.m_guardianPower.name)
            {
                return;
            }

            __instance.StartCoroutine(TrophyDropService.DropAfterDelay(__instance));
            GetMyTrophyBackPlugin.Log.LogDebug($"Scheduled trophy drop for {__instance.m_guardianPower.name}.");
        }
    }

    [HarmonyPatch(typeof(ItemStand), "Awake")]
    internal static class RegisterTrophyDropRpcPatch
    {
        private static void Postfix(ItemStand __instance, ZNetView ___m_nview)
        {
            if (__instance.m_guardianPower == null || ___m_nview == null ||
                ___m_nview.GetZDO() == null)
            {
                return;
            }

            ___m_nview.Register(
                TrophyDropService.RpcName,
                sender => TrophyDropService.HandleDropRequest(__instance, ___m_nview, sender));
            GetMyTrophyBackPlugin.Log.LogDebug($"Registered trophy drop RPC for {__instance.m_guardianPower.name}.");
        }
    }
}
