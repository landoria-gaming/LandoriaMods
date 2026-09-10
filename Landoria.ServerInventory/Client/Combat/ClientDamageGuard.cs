using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    internal static class ClientDamageGuard
    {
        internal static bool Active => ZNet.instance != null && !ZNet.instance.IsServer() &&
            ZNet.instance.IsCurrentServerDedicated() && CharacterLoad.Profile != null;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class ClientDamageRequestGuard
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class ClientDamageRpcGuard
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class ClientDamageApplicationGuard
    {
        private static bool Prefix() => !ClientDamageGuard.Active;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
    internal static class ClientHealthDecreaseGuard
    {
        private static bool Prefix(Character __instance, float health)
        {
            if (!ClientDamageGuard.Active || ItemAdditionCapture.Loading > 0 || ReferenceEquals(__instance, CombatResult.Applying)) return true;
            // Prevent bypasses that subtract health directly instead of using Damage.
            return health >= __instance.GetHealth();
        }
    }
}
