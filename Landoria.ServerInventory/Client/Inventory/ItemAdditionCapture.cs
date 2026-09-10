using System;
using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    internal static class ItemAdditionCapture
    {
        [ThreadStatic] internal static int Loading;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Load))]
    internal static class InventoryLoadCapturePatch
    {
        private static void Prefix() => ItemAdditionCapture.Loading++;
        private static Exception Finalizer(Exception __exception) { ItemAdditionCapture.Loading--; return __exception; }
    }

}
