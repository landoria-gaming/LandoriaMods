using System;
using HarmonyLib;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.TryRunCommand))]
    internal static class SpawnRequest
    {
        private static bool Prefix(Terminal __instance, string text)
        {
            if (!ClientDamageGuard.Active || string.IsNullOrWhiteSpace(text)) return true;
            var words = text.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
            if (!string.Equals(words[0], "spawn", StringComparison.OrdinalIgnoreCase)) return true;
            try { ZNet.instance.GetServerRPC().Invoke(CharacterRpc.Spawn, text); }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            return false;
        }

        internal static void Receive(ZRpc rpc, string message)
        {
            if (ZNet.instance == null || rpc != ZNet.instance.GetServerRPC()) return;
            Console.instance?.AddString(message);
        }
    }
}
