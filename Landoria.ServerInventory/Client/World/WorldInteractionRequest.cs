using System;
using HarmonyLib;
using UnityEngine;
using Landoria.ServerInventory.Server;

namespace Landoria.ServerInventory.Client
{
    internal static class WorldInteractionRequest
    {
        internal static bool Send(Component component, Humanoid user, ItemDrop.ItemData item, bool alt)
        {
            if (!ClientDamageGuard.Active || !NativeWorldInteraction.Supported(component)) return false;
            if (user != Player.m_localPlayer) return true;
            var view = component.GetComponentInParent<ZNetView>();
            if (view == null) return true;
            var request = InventoryActionRequest.Request(view, "interact");
            if (request == null) return true;
            request["component"] = Array.IndexOf(view.GetComponentsInChildren<MonoBehaviour>(true), component);
            request["alt"] = alt; request["useItem"] = item != null;
            if (item != null)
            {
                request["x"] = item.m_gridPos.x; request["y"] = item.m_gridPos.y;
                request["prefab"] = item.m_dropPrefab.name.GetStableHashCode();
            }
            WorldActionRequest.Send(request);
            return true;
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    internal static class PickableInteraction
    {
        private static bool Prefix(Pickable __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
        {
            if (__1 && ClientDamageGuard.Active) { __result = false; return false; }
            if (!WorldInteractionRequest.Send(__instance, __0, null, __2)) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.UseItem))]
    internal static class PickableUseItem
    {
        private static bool Prefix(Pickable __instance, Humanoid __0, ItemDrop.ItemData __1, ref bool __result)
        {
            if (!WorldInteractionRequest.Send(__instance, __0, __1, false)) return true;
            __result = true; return false;
        }
    }

    [HarmonyPatch(typeof(PickableItem), nameof(PickableItem.Interact))]
    internal static class PickableItemInteraction
    {
        private static bool Prefix(PickableItem __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
        {
            if (__1 && ClientDamageGuard.Active) { __result = false; return false; }
            if (!WorldInteractionRequest.Send(__instance, __0, null, __2)) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(PickableItem), nameof(PickableItem.UseItem))]
    internal static class PickableItemUseItem
    {
        private static bool Prefix(PickableItem __instance, Humanoid __0, ItemDrop.ItemData __1, ref bool __result)
        {
            if (!WorldInteractionRequest.Send(__instance, __0, __1, false)) return true;
            __result = true; return false;
        }
    }

    [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
    internal static class BeehiveInteraction
    {
        private static bool Prefix(Beehive __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
        {
            if (__1 && ClientDamageGuard.Active) { __result = false; return false; }
            if (!WorldInteractionRequest.Send(__instance, __0, null, __2)) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.UseItem))]
    internal static class BeehiveUseItem
    {
        private static bool Prefix(Beehive __instance, Humanoid __0, ItemDrop.ItemData __1, ref bool __result)
        {
            if (!WorldInteractionRequest.Send(__instance, __0, __1, false)) return true;
            __result = true; return false;
        }
    }

    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Interact))]
    internal static class FermenterInteraction
    {
        private static bool Prefix(Fermenter __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
        {
            if (__1 && ClientDamageGuard.Active) { __result = false; return false; }
            if (!WorldInteractionRequest.Send(__instance, __0, null, __2)) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.UseItem))]
    internal static class FermenterUseItem
    {
        private static bool Prefix(Fermenter __instance, Humanoid __0, ItemDrop.ItemData __1, ref bool __result)
        {
            if (!WorldInteractionRequest.Send(__instance, __0, __1, false)) return true;
            __result = true; return false;
        }
    }

    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Interact))]
    internal static class CookingStationInteraction
    {
        private static bool Prefix(CookingStation __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
        {
            if (__1 && ClientDamageGuard.Active) { __result = false; return false; }
            if (!WorldInteractionRequest.Send(__instance, __0, null, __2)) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UseItem))]
    internal static class CookingStationUseItem
    {
        private static bool Prefix(CookingStation __instance, Humanoid __0, ItemDrop.ItemData __1, ref bool __result)
        {
            if (!WorldInteractionRequest.Send(__instance, __0, __1, false)) return true;
            __result = true; return false;
        }
    }

    [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
    internal static class SwitchInteraction
    {
        private static bool Prefix(Switch __instance, Humanoid __0, bool __1, bool __2, ref bool __result)
        {
            if (__1 && ClientDamageGuard.Active) { __result = false; return false; }
            if (!WorldInteractionRequest.Send(__instance, __0, null, __2)) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(Switch), nameof(Switch.UseItem))]
    internal static class SwitchUseItem
    {
        private static bool Prefix(Switch __instance, Humanoid __0, ItemDrop.ItemData __1, ref bool __result)
        {
            if (!WorldInteractionRequest.Send(__instance, __0, __1, false)) return true;
            __result = true; return false;
        }
    }
}
