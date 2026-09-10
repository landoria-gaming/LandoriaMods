using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class CombatScene
    {
        internal static IEnumerable<Vector3> Centers()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) yield break;
            foreach (var peer in ZNet.instance.GetPeers())
            {
                if (!peer.IsReady() || peer.m_characterID.IsNone() || !InventoryChangesServer.IsLoaded(peer.m_rpc)) continue;
                var character = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (character != null) yield return character.GetPosition();
            }
        }

        internal static void AddObjects(List<ZDO> near, List<ZDO> distant)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return;
            var known = new HashSet<ZDO>(near);
            var far = new HashSet<ZDO>(distant);
            foreach (var center in Centers())
            {
                var additional = new List<ZDO>();
                ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(center), new SimulationDistance(1, 0), additional);
                foreach (var zdo in additional)
                {
                    if (known.Add(zdo)) near.Add(zdo);
                    if (far.Remove(zdo)) distant.Remove(zdo);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), "Update")]
    internal static class CombatZonesPatch
    {
        private static void Postfix(ZoneSystem __instance)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated() || !__instance.LocationsGenerated) return;
            try
            {
                foreach (var center in CombatScene.Centers()) CreateLocalZones(__instance, center);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZoneSystem), "CreateLocalZones")]
        private static bool CreateLocalZones(ZoneSystem instance, Vector3 refPoint)
            => throw new NotSupportedException("Native CreateLocalZones reverse patch was not installed.");
    }

    [HarmonyPatch(typeof(ZNetScene), "CreateObjects")]
    internal static class CombatObjectsPatch
    {
        private static void Prefix(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
            => CombatScene.AddObjects(currentNearObjects, currentDistantObjects);
    }
}
