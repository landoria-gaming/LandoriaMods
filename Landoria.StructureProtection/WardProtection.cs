using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Landoria.StructureProtection
{
    internal static class WardProtection
    {
        private static readonly HashSet<PrivateArea> Wards = new HashSet<PrivateArea>();

        private static bool ShouldBlockPlayerDamage(Vector3 position, long attacker)
        {
            foreach (PrivateArea ward in Wards)
            {
                if (!TryGetWardState(ward, position, out long creator, out List<long> permitted))
                {
                    continue;
                }
                if (!WardProtectionPolicy.IsAuthorized(creator, permitted, attacker))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetWardState(
            PrivateArea ward, Vector3 position, out long creator, out List<long> permitted)
        {
            creator = 0L;
            permitted = null;
            ZNetView view = ward != null ? ward.GetComponent<ZNetView>() : null;
            ZDO zdo = view?.GetZDO();
            if (zdo == null || !zdo.GetBool(ZDOVars.s_enabled) ||
                Utils.DistanceXZ(ward.transform.position, position) >= ward.m_radius)
            {
                return false;
            }
            Piece piece = ward.GetComponent<Piece>();
            creator = piece != null ? piece.GetCreator() : 0L;
            permitted = ReadPermittedPlayers(zdo);
            return creator != 0L;
        }

        private static List<long> ReadPermittedPlayers(ZDO zdo)
        {
            List<long> players = new List<long>();
            int count = zdo.GetInt(ZDOVars.s_permitted);
            for (int index = 0; index < count; index++)
            {
                long player = zdo.GetLong("pu_id" + index, 0L);
                if (player != 0L)
                {
                    players.Add(player);
                }
            }
            return players;
        }

        [HarmonyPatch(typeof(PrivateArea), "Awake")]
        private static class WardAwakePatch
        {
            private static void Postfix(PrivateArea __instance)
            {
                Wards.Add(__instance);
                WardQuota.Observe(__instance);
            }
        }

        [HarmonyPatch(typeof(PrivateArea), "OnDestroy")]
        private static class WardDestroyPatch
        {
            private static void Prefix(PrivateArea __instance)
            {
                Wards.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
        private static class PlayerDamagePatch
        {
            private static bool Prefix(WearNTear __instance, HitData hit)
            {
                Player attacker = hit?.GetAttacker() as Player;
                return !StructureProtectionPlugin.Settings.WardPlayerDamageEnabled ||
                       hit == null || hit.m_hitType != HitData.HitType.PlayerHit ||
                       !ShouldBlockPlayerDamage(
                           __instance.transform.position, attacker?.GetPlayerID() ?? 0L);
            }
        }
    }
}
