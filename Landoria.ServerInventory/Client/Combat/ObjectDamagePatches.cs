using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(TreeBase), "Damage")]
    internal static class TreeBaseDamagePatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(TreeBase), "RPC_Damage")]
    internal static class TreeBaseDamageRpcPatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(TreeLog), "Damage")]
    internal static class TreeLogDamagePatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(TreeLog), "RPC_Damage")]
    internal static class TreeLogDamageRpcPatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(Destructible), "Damage")]
    internal static class DestructibleDamagePatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(Destructible), "RPC_Damage")]
    internal static class DestructibleDamageRpcPatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(WearNTear), "Damage")]
    internal static class WearNTearDamagePatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
    internal static class WearNTearDamageRpcPatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(MineRock), "Damage")]
    internal static class MineRockDamagePatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(MineRock), "RPC_Hit")]
    internal static class MineRockDamageRpcPatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(MineRock5), "Damage")]
    internal static class MineRock5DamagePatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(MineRock5), "RPC_Damage")]
    internal static class MineRock5DamageRpcPatch
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
    internal static class StructureDamageApplicationGuard
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            __result = false;
            return false;
        }
    }
}
