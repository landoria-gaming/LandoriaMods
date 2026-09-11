using System;
using HarmonyLib;

namespace Landoria.Moderator
{
    [HarmonyPatch(typeof(Terminal.ConsoleCommand), MethodType.Constructor, new[]
    {
        typeof(string), typeof(string), typeof(Terminal.ConsoleEvent),
        typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
        typeof(bool), typeof(Terminal.ConsoleOptionsFetcher), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class ConsoleEventCommandConstructorPatch
    {
        private static bool Prefix(string command)
        {
            return !string.Equals(command, "devcommands", StringComparison.OrdinalIgnoreCase);
        }
    }

    [HarmonyPatch(typeof(Terminal.ConsoleCommand), MethodType.Constructor, new[]
    {
        typeof(string), typeof(string), typeof(Terminal.ConsoleEventFailable),
        typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
        typeof(bool), typeof(Terminal.ConsoleOptionsFetcher), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class FailableCommandConstructorPatch
    {
        private static bool Prefix(string command)
        {
            return !string.Equals(command, "devcommands", StringComparison.OrdinalIgnoreCase);
        }
    }

    [HarmonyPatch(typeof(Terminal.ConsoleCommand), "ShowCommand")]
    internal static class HideDevCommandsPatch
    {
        private static void Postfix(Terminal.ConsoleCommand __instance, ref bool __result)
        {
            if (__instance.IsCheat)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Terminal.ConsoleCommand), "RunAction")]
    internal static class BlockDevCommandsPatch
    {
        private static bool Prefix(Terminal.ConsoleCommand __instance)
        {
            return !__instance.IsCheat;
        }
    }
}
