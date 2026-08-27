using HarmonyLib;
using UnityEngine;

namespace Landoria.HammerFreedom
{
    [HarmonyPatch(typeof(Player), "OnSpawned")]
    internal static class HammerFreedomAuthorizationOnSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                HammerFreedomAuthorization.RequestOnSpawn();
            }
        }
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class FlyCommandRegistrationPatch
    {
        private static void Postfix()
        {
            FlyCommand.Register();
        }
    }

    [HarmonyPatch(typeof(Terminal.ConsoleCommand), "IsValid")]
    internal static class FlyCommandValidationPatch
    {
        private static void Postfix(Terminal.ConsoleCommand __instance, ref bool __result)
        {
            if (FlyCommand.IsCommand(__instance) && !HammerFreedomAuthorization.IsAuthorized(
                    HammerFreedomCapabilities.Flight))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "Damage")]
    internal static class FallDamagePatch
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            return HammerFreedomBehaviorPolicy.ShouldApplyDamage(
                __instance == Player.m_localPlayer,
                hit.m_hitType == HitData.HitType.Fall,
                HammerFreedomAuthorization.IsAuthorized(
                    HammerFreedomCapabilities.FallDamageImmunity));
        }
    }

    [HarmonyPatch(typeof(Player), "UseStamina")]
    internal static class StaminaConsumptionPatch
    {
        private static bool Prefix(Player __instance)
        {
            return HammerFreedomBehaviorPolicy.ShouldConsumeStamina(
                __instance == Player.m_localPlayer,
                HammerFreedomAuthorization.IsAuthorized(
                    HammerFreedomCapabilities.UnlimitedStamina));
        }
    }

    [HarmonyPatch(typeof(Player), "RPC_UseStamina")]
    internal static class StaminaApplicationPatch
    {
        private static bool Prefix(Player __instance)
        {
            return HammerFreedomBehaviorPolicy.ShouldConsumeStamina(
                __instance == Player.m_localPlayer,
                HammerFreedomAuthorization.IsAuthorized(
                    HammerFreedomCapabilities.UnlimitedStamina));
        }
    }

    internal struct DurabilitySnapshot
    {
        private readonly ItemDrop.ItemData _item;
        private readonly float _durability;

        internal DurabilitySnapshot(ItemDrop.ItemData item, bool preserve)
        {
            _item = preserve ? item : null;
            _durability = item?.m_durability ?? 0f;
        }

        internal void Restore()
        {
            if (_item != null)
            {
                _item.m_durability = _durability;
            }
        }
    }

    internal static class DurabilityProtection
    {
        internal static bool IsActive(Humanoid humanoid)
        {
            return HammerFreedomBehaviorPolicy.ShouldPreserveDurability(
                humanoid == Player.m_localPlayer,
                HammerFreedomAuthorization.IsAuthorized(
                    HammerFreedomCapabilities.NoDurabilityLoss));
        }
    }

    [HarmonyPatch(typeof(Attack), "OnAttackTrigger")]
    internal static class AttackDurabilityPatch
    {
        private static void Prefix(Humanoid ___m_character, ItemDrop.ItemData ___m_weapon,
            out DurabilitySnapshot __state)
        {
            __state = new DurabilitySnapshot(
                ___m_weapon, DurabilityProtection.IsActive(___m_character));
        }

        private static void Postfix(DurabilitySnapshot __state)
        {
            __state.Restore();
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    internal static class PlacementDurabilityPatch
    {
        private static void Prefix(Player __instance, ItemDrop.ItemData ___m_rightItem,
            out DurabilitySnapshot __state)
        {
            __state = new DurabilitySnapshot(
                ___m_rightItem, DurabilityProtection.IsActive(__instance));
        }

        private static void Postfix(DurabilitySnapshot __state)
        {
            __state.Restore();
        }
    }

    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    internal static class BlockDurabilityPatch
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData ___m_rightItem,
            ItemDrop.ItemData ___m_leftItem, out DurabilitySnapshot[] __state)
        {
            bool preserve = DurabilityProtection.IsActive(__instance);
            __state = new[]
            {
                new DurabilitySnapshot(___m_rightItem, preserve),
                new DurabilitySnapshot(___m_leftItem, preserve)
            };
        }

        private static void Postfix(DurabilitySnapshot[] __state)
        {
            foreach (DurabilitySnapshot snapshot in __state)
            {
                snapshot.Restore();
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), "DrainEquipedItemDurability")]
    internal static class EquippedDurabilityPatch
    {
        private static bool Prefix(Humanoid __instance)
        {
            return !DurabilityProtection.IsActive(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "DamageArmorDurability")]
    internal static class ArmorDurabilityPatch
    {
        private static bool Prefix(Player __instance)
        {
            return !DurabilityProtection.IsActive(__instance);
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateDebugFly")]
    internal static class FlightSpeedPatch
    {
        private static void Postfix(
            Character __instance, bool ___m_run, ref Vector3 ___m_currentVel,
            Rigidbody ___m_body)
        {
            if (__instance != Player.m_localPlayer ||
                !HammerFreedomAuthorization.IsAuthorized(HammerFreedomCapabilities.Flight))
            {
                return;
            }

            float scale = FlightSpeedPolicy.ResolveScale(
                ___m_currentVel.magnitude, ___m_run);
            if (scale < 1f)
            {
                ___m_currentVel *= scale;
                ___m_body.linearVelocity = ___m_currentVel;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "Jump")]
    internal static class FlyingJumpPatch
    {
        private static bool Prefix(Character __instance)
        {
            return FlyInput.ShouldApplyGroundAction(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "SetCrouch")]
    internal static class FlyingCrouchPatch
    {
        private static bool Prefix(Player __instance, bool crouch)
        {
            return !crouch || FlyInput.ShouldApplyGroundAction(__instance);
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class HammerFreedomDisconnectPatch
    {
        private static void Prefix()
        {
            HammerFreedomAuthorization.ResetSession();
        }
    }
}
