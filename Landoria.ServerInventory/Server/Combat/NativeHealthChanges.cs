using System;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class NativePeriodicDamagePatch
    {
        internal sealed class State
        {
            internal float Before;
            internal bool Previous;
            internal bool Publish;
        }
        private static void Prefix(Character __instance, out State __state)
        {
            __state = new State { Before = __instance.GetHealth(), Previous = NativeDamage.Executing };
            if (ZNet.instance == null || !ZNet.instance.IsDedicated() || NativeDamage.Executing) return;
            __state.Publish = true;
            NativeDamage.Executing = true;
        }

        private static Exception Finalizer(Character __instance, HitData hit, State __state, Exception __exception)
        {
            if (__state == null) return __exception;
            NativeDamage.Executing = __state.Previous;
            if (__exception != null || !__state.Publish || __instance.GetHealth().Equals(__state.Before)) return __exception;
            try { NativeDamage.Publish(__instance, hit, __state.Before); }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
    internal static class NativeHealthChangePatch
    {
        private static void Prefix(Character __instance, out float __state) => __state = __instance.GetHealth();

        private static void Postfix(Character __instance, float __state)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated() || !CombatScope.Active ||
                NativeDamage.Executing || CombatCharacters.Preparing || __instance.GetHealth().Equals(__state)) return;
            try { NativeDamage.Publish(__instance, new HitData(), __state); }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.InAttack))]
    internal static class ServerEquipmentRestorePatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!CombatCharacters.Preparing) return true;
            __result = false;
            return false;
        }
    }
}
