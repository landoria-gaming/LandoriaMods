using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Landoria.ServerInventory.Server
{
    internal sealed class CombatScope : IDisposable
    {
        [ThreadStatic] private static HashSet<ZDOID> owners;
        private readonly HashSet<ZDOID> previous;
        internal static bool Active => owners != null;
        internal CombatScope(Character character)
        {
            previous = owners;
            owners = previous == null ? new HashSet<ZDOID>() : new HashSet<ZDOID>(previous);
            owners.Add(character.GetZDOID());
        }
        internal static bool Owns(ZDO zdo) => zdo != null && owners != null && owners.Contains(zdo.m_uid);
        public void Dispose() => owners = previous;
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.IsOwner))]
    internal static class CombatOwnershipPatch
    {
        private static bool Prefix(ZDO __instance, ref bool __result)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated() || !CombatScope.Owns(__instance)) return true;
            // Only native combat execution gets temporary local authority; ownership is never transferred.
            __result = true;
            return false;
        }
    }
}
