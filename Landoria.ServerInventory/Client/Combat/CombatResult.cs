using System;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    internal static class CombatResult
    {
        [ThreadStatic] internal static Character Applying;
        [ThreadStatic] internal static HitData Hit;

        internal static void ReceiveInventory(ZRpc rpc, ZPackage package)
        {
            if (ZNet.instance == null || rpc != ZNet.instance.GetServerRPC() || Player.m_localPlayer == null) return;
            try
            {
                ItemAdditionCapture.Loading++;
                InventorySnapshot.Apply(Player.m_localPlayer, package);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            finally { ItemAdditionCapture.Loading--; }
        }

        internal static void Receive(ZRpc rpc, ZPackage package)
        {
            if (ZNet.instance == null || rpc != ZNet.instance.GetServerRPC()) return;
            try
            {
                var id = package.ReadZDOID();
                float health = package.ReadSingle(), maximum = package.ReadSingle(), damage = package.ReadSingle();
                var hit = new HitData();
                hit.Deserialize(ref package);
                var instance = ZNetScene.instance.FindInstance(id);
                var target = instance == null ? null : instance.GetComponent<Character>();
                if (target == null) return;
                if (float.IsNaN(health) || float.IsNaN(maximum) || float.IsNaN(damage) ||
                    health < 0 || maximum <= 0 || health > maximum || damage < 0) return;
                if (target.IsOwner())
                {
                    Applying = target;
                    Hit = hit;
                    target.SetMaxHealth(maximum);
                    target.SetHealth(health);
                }
                if (damage > 0) DamageText.instance?.ShowText(HitData.DamageModifier.Normal, target.GetCenterPoint(), damage, target.IsPlayer());
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            finally { Applying = null; Hit = null; }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
    internal static class CombatLastHitPatch
    {
        private static void Prefix(Character __instance, ref HitData ___m_lastHit)
        {
            if (ReferenceEquals(__instance, CombatResult.Applying) && CombatResult.Hit != null)
                ___m_lastHit = CombatResult.Hit;
        }
    }
}
