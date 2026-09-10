using HarmonyLib;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.StartWithoutAnimation))]
    internal static class NativeAttackInitialization
    {
        private static void Prefix(Humanoid character, ref int ___m_attackMask, ref int ___m_attackMaskTerrain,
            ref int ___m_attackMaskCharacters, ref int ___m_harvestRayMask, ref int ___m_snowShovelRayMask,
            ref ZSyncAnimation ___m_zanim, ref CharacterAnimEvent ___m_animEvent)
        {
            if (!CombatScope.Active) return;
            // StartWithoutAnimation skips the masks normally initialized by Attack.Start.
            ___m_attackMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid",
                "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
            ___m_attackMaskTerrain = ___m_attackMask | LayerMask.GetMask("terrain");
            ___m_attackMaskCharacters = LayerMask.GetMask("character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
            ___m_harvestRayMask = LayerMask.GetMask("piece", "piece_nonsolid", "item");
            ___m_snowShovelRayMask = ___m_harvestRayMask | LayerMask.GetMask("terrain");
            ___m_zanim = character.GetComponent<ZSyncAnimation>();
            ___m_animEvent = character.GetComponentInChildren<CharacterAnimEvent>();
        }
    }
}
