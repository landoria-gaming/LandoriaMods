using HarmonyLib;

namespace Landoria.DecayControl
{
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class ShowDecayCommandRegistrationPatch
    {
        private static void Postfix()
        {
            ShowDecayCommand.Register();
        }
    }

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    internal static class DecayStateOnSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                DecayStateRpc.RequestOnSpawn();
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class DecayControlDisconnectPatch
    {
        private static void Prefix()
        {
            DecayStateRpc.ResetSession();
            DecayIndicators.Reset();
        }
    }
}
