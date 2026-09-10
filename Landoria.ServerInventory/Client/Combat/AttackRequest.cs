using System;
using HarmonyLib;
using Landoria.ServerInventory.Network;
using UnityEngine;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
    internal static class AttackRequest
    {
        private static bool Prefix(Attack __instance, Humanoid ___m_character, ItemDrop.ItemData ___m_weapon, float ___m_attackDrawPercentage)
        {
            if (!ClientDamageGuard.Active || __instance.m_attackType == Attack.AttackType.None) return true;
            if (___m_character == null || !___m_character.IsOwner() || ___m_weapon == null) return false;
            try
            {
                var package = new ZPackage();
                string request = Guid.NewGuid().ToString("D");
                package.Write(request);
                package.Write(___m_character.GetZDOID());
                string weaponName = ___m_weapon.m_dropPrefab != null ? ___m_weapon.m_dropPrefab.name : ___m_character.m_unarmedWeapon?.name;
                if (string.IsNullOrEmpty(weaponName)) return false;
                package.Write(weaponName.GetStableHashCode());
                package.Write(___m_weapon.m_quality);
                package.Write(__instance.m_attackAnimation == ___m_weapon.m_shared.m_secondaryAttack.m_attackAnimation);
                package.Write(Mathf.Clamp01(___m_attackDrawPercentage));
                PredictedImpact.Play(request, __instance, ___m_character, ___m_weapon);
                ZNet.instance.GetServerRPC().Invoke(CharacterRpc.Attack, package);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            // Only the server executes hits and creates attack projectiles.
            return false;
        }
    }
}
