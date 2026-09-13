using System;
using HarmonyLib;

namespace Landoria.StructureProtection
{
    internal static class CreatureProtection
    {
        private static float GetActivity(StaticTarget target)
        {
            Piece piece = target?.GetComponentInParent<Piece>();
            return PieceActivity.GetMultiplier(piece);
        }

        [HarmonyPatch(typeof(StaticTarget), nameof(StaticTarget.IsPriorityTarget))]
        private static class PriorityTargetPatch
        {
            private static void Postfix(StaticTarget __instance, ref bool __result)
            {
                __result = CreatureProtectionPolicy.CanTarget(
                    StructureProtectionPlugin.Settings.CreatureTargetingEnabled,
                    __result, GetActivity(__instance));
            }
        }

        [HarmonyPatch(typeof(StaticTarget), nameof(StaticTarget.IsRandomTarget))]
        private static class RandomTargetPatch
        {
            private static void Postfix(StaticTarget __instance, ref bool __result)
            {
                __result = CreatureProtectionPolicy.CanTarget(
                    StructureProtectionPlugin.Settings.CreatureTargetingEnabled,
                    __result, GetActivity(__instance));
            }
        }

        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanSeeTarget), new Type[] { typeof(StaticTarget) })]
        private static class VisibilityPatch
        {
            private static void Postfix(StaticTarget target, ref bool __result)
            {
                __result = CreatureProtectionPolicy.CanTarget(
                    StructureProtectionPlugin.Settings.CreatureTargetingEnabled,
                    __result, GetActivity(target));
            }
        }
    }
}
