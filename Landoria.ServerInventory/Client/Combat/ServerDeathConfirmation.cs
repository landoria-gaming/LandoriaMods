using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(Character), "CheckDeath")]
    internal static class ServerDeathConfirmation
    {
        private sealed class State { internal bool Dead; }
        private static readonly ConditionalWeakTable<Character, State> states = new ConditionalWeakTable<Character, State>();

        internal static void Record(Character character, float health)
            => states.GetOrCreateValue(character).Dead = health <= 0f;

        private static bool Prefix(Character __instance)
        {
            if (!ClientDamageGuard.Active || !(__instance is Player) || !__instance.IsOwner()) return true;
            // A loaded save can still contain the previous death until the server completes respawning.
            return ItemAdditionCapture.Loading == 0 && states.TryGetValue(__instance, out var state) && state.Dead;
        }
    }
}
