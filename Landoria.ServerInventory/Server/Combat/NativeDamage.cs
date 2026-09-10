using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    internal static class NativeDamage
    {
        [ThreadStatic] internal static bool Executing;
        private static readonly HashSet<Character> affected = new HashSet<Character>();

        internal static void Apply(Character target, HitData hit)
        {
            bool previous = Executing;
            try
            {
                using (var scope = new CombatScope(target))
                {
                    CombatCharacters.Prepare(target);
                    float before = target.GetHealth();
                    var package = new ZPackage();
                    hit.Serialize(ref package);
                    package.SetPos(0);
                    var data = new ZRoutedRpc.RoutedRPCData { m_senderPeerID = ZNet.GetUID(),
                        m_methodHash = "RPC_Damage".GetStableHashCode(), m_parameters = package };
                    affected.Add(target);
                    Executing = true;
                    target.GetComponent<ZNetView>().HandleRoutedRPC(data);
                    Publish(target, hit, before);
                    if (target is Humanoid humanoid) CombatCharacters.SaveInventory(humanoid);
                }
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            finally { Executing = previous; }
        }

        internal static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) { affected.Clear(); return; }
            foreach (var target in affected.ToArray())
            {
                if (target == null || target.IsDead()) { affected.Remove(target); continue; }
                var zdo = ZDOMan.instance.GetZDO(target.GetZDOID());
                if (zdo == null || zdo.GetOwner() == ZNet.GetUID()) continue;
                try
                {
                    using (var scope = new CombatScope(target))
                        target.GetSEMan().Update(zdo, Time.deltaTime);
                }
                catch (Exception error) { CharacterRpc.Log.LogError(error); affected.Remove(target); }
            }
        }

        internal static void Publish(Character target, HitData hit, float before)
        {
            CombatCharacters.SaveHealth(target);
            var package = new ZPackage();
            package.Write(target.GetZDOID());
            package.Write(target.GetHealth());
            package.Write(target.GetMaxHealth());
            package.Write(Math.Max(0f, before - target.GetHealth()));
            hit.Serialize(ref package);
            foreach (var peer in ZNet.instance.GetPeers())
                if (peer.IsReady()) peer.m_rpc.Invoke(CharacterRpc.CombatResult, package);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class ServerNativeDamagePatch
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return true;
            NativeDamage.Apply(__instance, hit);
            return false;
        }
    }

    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class ServerDamageRpcGuard
    {
        private static bool Prefix()
            => ZNet.instance == null || !ZNet.instance.IsDedicated() || NativeDamage.Executing;
    }
}
