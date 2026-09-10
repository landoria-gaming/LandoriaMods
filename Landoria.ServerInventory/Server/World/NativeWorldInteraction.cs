using System;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    internal static class NativeWorldInteraction
    {
        internal static bool Supported(Component component)
            => component is Pickable || component is PickableItem || component is Beehive || component is Fermenter ||
                component is CookingStation || (component is Switch &&
                (component.GetComponentInParent<Smelter>() != null || component.GetComponentInParent<CookingStation>() != null));

        internal static void Apply(WorldActionTransaction action, JObject request)
        {
            var view = WorldInventoryActions.Target(action, request);
            var components = view.GetComponentsInChildren<MonoBehaviour>(true);
            int index = (int)request["component"];
            if (index < 0 || index >= components.Length || !Supported(components[index]))
                throw new InvalidDataException("Unsupported world interaction.");
            var target = components[index] as Interactable;
            if (target == null) throw new InvalidDataException("Object is not interactive.");
            var previous = Player.m_localPlayer;
            Player.m_localPlayer = action.Player;
            try
            {
                if ((bool?)request["useItem"] == true)
                {
                    var item = action.Inventory.GetItemAt((int)request["x"], (int)request["y"]);
                    if (item == null || item.m_dropPrefab.name.GetStableHashCode() != (int)request["prefab"])
                        throw new InvalidOperationException("Item is missing or changed.");
                    target.UseItem(action.Player, item);
                }
                else target.Interact(action.Player, false, (bool?)request["alt"] == true);
            }
            finally { Player.m_localPlayer = previous; }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetInventory))]
    internal static class ServerActionInventory
    {
        private static bool Prefix(Humanoid __instance, ref Inventory __result)
        {
            var action = WorldActionTransaction.Current;
            if (action == null || action.Player != __instance) return true;
            __result = action.Inventory; return false;
        }
    }
    [HarmonyPatch(typeof(Player), nameof(Player.NoCostCheat))]
    internal static class ServerActionCosts
    {
        private static bool Prefix(ref bool __result)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return true;
            __result = false; return false;
        }
    }
}
