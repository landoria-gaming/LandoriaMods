using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Landoria.SharedLib;
using UnityEngine;

namespace Landoria.DecayControl
{
    internal static class DecayProtection
    {
        private const float VanillaRainDamageFraction = 0.05f;
        private static readonly HashSet<long> ActiveCreators = new HashSet<long>();
        private static ConditionalWeakTable<Fireplace, object> initializedFireplaces =
            new ConditionalWeakTable<Fireplace, object>();
        private static bool hasServerState;

        internal static void Reset()
        {
            ActiveCreators.Clear();
            hasServerState = false;
            initializedFireplaces = new ConditionalWeakTable<Fireplace, object>();
        }

        internal static void WriteState(ZPackage package, IEnumerable<long> onlinePlayers)
        {
            HashSet<long> activeCreators =
                CreatorActivityPolicy.GetActiveCreators(onlinePlayers);
            package.Write(activeCreators.Count);
            foreach (long playerId in activeCreators)
            {
                package.Write(playerId);
            }
        }

        internal static void ReadState(ZPackage package)
        {
            ActiveCreators.Clear();
            int count = package.ReadInt();
            for (int index = 0; index < count; index++)
            {
                ActiveCreators.Add(package.ReadLong());
            }
            hasServerState = true;
        }

        internal static void ClearActivityState()
        {
            ActiveCreators.Clear();
            hasServerState = false;
        }

        internal static int ActiveCreatorCount => ActiveCreators.Count;

        internal static bool IsFuelDecayOff(Piece piece)
        {
            if (piece == null || !piece.IsPlacedByPlayer())
            {
                return false;
            }
            DecayControlMode mode = DecayControlPlugin.Settings.FuelConsumption;
            return mode == DecayControlMode.Disabled ||
                (mode == DecayControlMode.PlayerOnline &&
                 GetActivityMultiplier(piece) <= 0f);
        }

        internal static bool IsEnvironmentalWearOff(Piece piece)
        {
            if (piece == null || !piece.IsPlacedByPlayer())
            {
                return false;
            }
            DecayControlMode mode =
                DecayControlPlugin.Settings.EnvironmentalBuildingWear;
            return mode == DecayControlMode.Disabled ||
                (mode == DecayControlMode.PlayerOnline &&
                 GetActivityMultiplier(piece) <= 0f);
        }

        internal static float GetActivityMultiplier(Piece piece)
        {
            if (piece == null || !piece.IsPlacedByPlayer())
            {
                return 1f;
            }
            long creator = piece.GetCreator();
            if (creator == 0L)
            {
                return 1f;
            }
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                return IsCreatorActive(
                    creator, DecayStateRpc.GetOnlinePlayers()) ? 1f : 0f;
            }
            return !hasServerState || ActiveCreators.Contains(creator) ? 1f : 0f;
        }

        private static bool IsCreatorActive(long creator, HashSet<long> onlinePlayers)
        {
            return CreatorActivityPolicy.IsCreatorActive(creator, onlinePlayers);
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
        private static class RainDamagePatch
        {
            private static bool Prefix(WearNTear __instance, float damage, HitData hitData)
            {
                DecayControlMode mode =
                    DecayControlPlugin.Settings.EnvironmentalBuildingWear;
                if (mode != DecayControlMode.PlayerOnline)
                {
                    return true;
                }
                float rainDamage = __instance.m_health * VanillaRainDamageFraction;
                bool isVanillaRainTick = hitData == null && __instance.IsWet() &&
                    __instance.GetHealthPercentage() > 0.5f &&
                    Mathf.Approximately(damage, rainDamage);
                Piece piece = __instance.GetComponent<Piece>();
                bool isPlayerBuilt = piece != null && piece.IsPlacedByPlayer();
                float activity = GetActivityMultiplier(piece);
                return DecayEffectPolicy.ShouldApplyEnvironmentalWear(isVanillaRainTick,
                    isPlayerBuilt, mode, activity);
            }
        }

        [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
        private static class NativeWearPatch
        {
            private static void Prefix(WearNTear __instance)
            {
                DecayControlMode mode =
                    DecayControlPlugin.Settings.EnvironmentalBuildingWear;
                if (mode != DecayControlMode.Disabled)
                {
                    return;
                }
                Piece piece = __instance.GetComponent<Piece>();
                bool isPlayerBuilt = piece != null && piece.IsPlacedByPlayer();
                if (DecayEffectPolicy.ShouldDisableNativeRoofWear(isPlayerBuilt, mode))
                {
                    __instance.m_noRoofWear = false;
                }
            }
        }

        [HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
        private static class FireplaceFuelPatch
        {
            private static void Prefix(Fireplace __instance, out float __state)
            {
                __state = __instance.m_secPerFuel;
                DecayControlMode mode = DecayControlPlugin.Settings.FuelConsumption;
                if (mode == DecayControlMode.Default)
                {
                    return;
                }
                Piece piece = __instance.GetComponent<Piece>();
                bool isPlayerBuilt = piece != null && piece.IsPlacedByPlayer();
                if (DecayEffectPolicy.ShouldUseNativeInfiniteFuel(isPlayerBuilt, mode))
                {
                    __instance.m_infiniteFuel = true;
                    return;
                }
                bool firstUpdate = isPlayerBuilt &&
                    !initializedFireplaces.TryGetValue(__instance, out _);
                if (firstUpdate)
                {
                    initializedFireplaces.Add(__instance, new object());
                }
                float activity = GetActivityMultiplier(piece);
                if (DecayEffectPolicy.ShouldPauseFuel(isPlayerBuilt, firstUpdate,
                    mode, activity))
                {
                    __instance.m_secPerFuel = float.PositiveInfinity;
                }
            }

            private static void Postfix(Fireplace __instance, float __state)
            {
                __instance.m_secPerFuel = __state;
            }
        }
    }
}
