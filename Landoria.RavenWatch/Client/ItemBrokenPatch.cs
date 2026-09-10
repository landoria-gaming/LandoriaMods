using System;
using HarmonyLib;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    [HarmonyPatch(typeof(Humanoid), "DrainEquipedItemDurability")]
    internal static class ItemBrokenPatch
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out bool __state)
        {
            __state = __instance == Player.m_localPlayer && item != null && item.m_equipped;
        }

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __state)
        {
            if (!__state || item.m_durability > 0f || item.m_equipped) return;
            try { ItemBrokenRpc.Send((Player)__instance, item); }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }
    }
}
