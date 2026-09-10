using HarmonyLib;
using System;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch(typeof(TreeBase), "Damage")]
    internal static class TreeBaseDamagePatch
    {
        private static bool Prefix(TreeBase __instance, out ObjectDamageScope __state)
        {
            __state = null;
            return !ObjectDamageScope.Dedicated || ObjectDamageScope.Begin(__instance, out __state);
        }

        private static Exception Finalizer(Exception __exception, ObjectDamageScope __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(TreeBase), "RPC_Damage")]
    internal static class TreeBaseDamageRpcPatch
    {
        private static bool Prefix(TreeBase __instance, long sender)
            => ObjectDamageScope.AcceptRpc(__instance, sender);
    }

    [HarmonyPatch(typeof(TreeLog), "Damage")]
    internal static class TreeLogDamagePatch
    {
        private static bool Prefix(TreeLog __instance, out ObjectDamageScope __state)
        {
            __state = null;
            return !ObjectDamageScope.Dedicated || ObjectDamageScope.Begin(__instance, out __state);
        }

        private static Exception Finalizer(Exception __exception, ObjectDamageScope __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(TreeLog), "RPC_Damage")]
    internal static class TreeLogDamageRpcPatch
    {
        private static bool Prefix(TreeLog __instance, long sender)
            => ObjectDamageScope.AcceptRpc(__instance, sender);
    }

    [HarmonyPatch(typeof(Destructible), "Damage")]
    internal static class DestructibleDamagePatch
    {
        private static bool Prefix(Destructible __instance, out ObjectDamageScope __state)
        {
            __state = null;
            return !ObjectDamageScope.Dedicated || ObjectDamageScope.Begin(__instance, out __state);
        }

        private static Exception Finalizer(Exception __exception, ObjectDamageScope __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Destructible), "RPC_Damage")]
    internal static class DestructibleDamageRpcPatch
    {
        private static bool Prefix(Destructible __instance, long sender)
            => ObjectDamageScope.AcceptRpc(__instance, sender);
    }

    [HarmonyPatch(typeof(WearNTear), "Damage")]
    internal static class WearNTearDamagePatch
    {
        private static bool Prefix(WearNTear __instance, out ObjectDamageScope __state)
        {
            __state = null;
            return !ObjectDamageScope.Dedicated || ObjectDamageScope.Begin(__instance, out __state);
        }

        private static Exception Finalizer(Exception __exception, ObjectDamageScope __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
    internal static class WearNTearDamageRpcPatch
    {
        private static bool Prefix(WearNTear __instance, long sender)
            => ObjectDamageScope.AcceptRpc(__instance, sender);
    }

    [HarmonyPatch(typeof(MineRock), "Damage")]
    internal static class MineRockDamagePatch
    {
        private static bool Prefix(MineRock __instance, out ObjectDamageScope __state)
        {
            __state = null;
            return !ObjectDamageScope.Dedicated || ObjectDamageScope.Begin(__instance, out __state);
        }

        private static Exception Finalizer(Exception __exception, ObjectDamageScope __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(MineRock), "RPC_Hit")]
    internal static class MineRockDamageRpcPatch
    {
        private static bool Prefix(MineRock __instance, long sender)
            => ObjectDamageScope.AcceptRpc(__instance, sender);
    }

    [HarmonyPatch(typeof(MineRock5), "Damage")]
    internal static class MineRock5DamagePatch
    {
        private static bool Prefix(MineRock5 __instance, out ObjectDamageScope __state)
        {
            __state = null;
            return !ObjectDamageScope.Dedicated || ObjectDamageScope.Begin(__instance, out __state);
        }

        private static Exception Finalizer(Exception __exception, ObjectDamageScope __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(MineRock5), "RPC_Damage")]
    internal static class MineRock5DamageRpcPatch
    {
        private static bool Prefix(MineRock5 __instance, long sender)
            => ObjectDamageScope.AcceptRpc(__instance, sender);
    }

}
