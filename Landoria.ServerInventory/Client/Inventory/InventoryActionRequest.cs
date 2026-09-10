using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Client
{
    internal static class InventoryActionRequest
    {
        private static readonly ConditionalWeakTable<Inventory, Container> containers = new ConditionalWeakTable<Inventory, Container>();
        internal static void Remember(Container container, Inventory inventory)
        {
            if (inventory != null && !containers.TryGetValue(inventory, out _)) containers.Add(inventory, container);
        }
        internal static JObject Request(Component target, string kind)
        {
            var view = target.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return null;
            var id = view.GetZDO().m_uid;
            return new JObject { ["kind"] = kind, ["user"] = id.UserID.ToString(), ["object"] = id.ID };
        }
        internal static void Pickup(ItemDrop drop, bool automatic = false)
        {
            if (automatic && (!drop.m_autoPickup || drop.IsPiece() || drop.InTar() ||
                !Player.m_localPlayer.GetInventory().CanAddItem(drop.m_itemData) ||
                drop.m_itemData.GetWeight() + Player.m_localPlayer.GetInventory().GetTotalWeight() > Player.m_localPlayer.GetMaxCarryWeight())) return;
            var request = Request(drop, "pickup");
            if (request != null) { request["automatic"] = automatic; WorldActionRequest.Send(request); }
        }
        internal static void Open(Container container)
        {
            var request = Request(container, "container");
            if (request == null) return;
            request["operation"] = "open";
            WorldActionRequest.Send(request);
        }
        internal static bool Transfer(Inventory destination, Inventory source, ItemDrop.ItemData item, int amount, string operation)
        {
            if (!ClientDamageGuard.Active || ItemAdditionCapture.Loading > 0 || destination == source) return false;
            var player = Player.m_localPlayer.GetInventory();
            bool deposit = source == player;
            var other = deposit ? destination : source;
            if (!containers.TryGetValue(other, out var container) || (!deposit && destination != player)) return true;
            var request = Request(container, "container");
            if (request == null) return true;
            request["operation"] = operation; request["deposit"] = deposit;
            if (item != null)
            {
                request["x"] = item.m_gridPos.x; request["y"] = item.m_gridPos.y;
                request["amount"] = amount; request["prefab"] = item.m_dropPrefab.name.GetStableHashCode();
            }
            WorldActionRequest.Send(request);
            return true;
        }
        internal static void ReceiveContainer(ZRpc rpc, ZDOID id, bool open, ZPackage package)
        {
            if (ZNet.instance == null || rpc != ZNet.instance.GetServerRPC()) return;
            ItemAdditionCapture.Loading++;
            try
            {
                var container = ZNetScene.instance.FindInstance(id)?.GetComponent<Container>();
                if (container == null) return;
                container.GetInventory().Load(package);
                if (open) InventoryGui.instance.Show(container);
            }
            catch (Exception error) { CharacterRpc.Log.LogError(error); }
            finally { ItemAdditionCapture.Loading--; }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetInventory))]
    internal static class ContainerInventoryMap
    {
        private static void Postfix(Container __instance, Inventory __result) => InventoryActionRequest.Remember(__instance, __result);
    }
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class ContainerOpenRequest
    {
        private static bool Prefix(Container __instance, Humanoid character, bool hold, ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            if (!hold && character == Player.m_localPlayer) InventoryActionRequest.Open(__instance);
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(Container), nameof(Container.IsOwner))]
    internal static class ContainerLocalDisplay
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            __result = true; return false;
        }
    }
    [HarmonyPatch(typeof(Container), nameof(Container.StackAll))]
    internal static class ContainerStackRequest
    {
        private static bool Prefix(Container __instance)
        {
            if (!ClientDamageGuard.Active) return true;
            InventoryActionRequest.Transfer(__instance.GetInventory(), Player.m_localPlayer.GetInventory(), null, 0, "stack");
            return false;
        }
    }
    [HarmonyPatch(typeof(Container), nameof(Container.TakeAll))]
    internal static class ContainerTakeAllRequest
    {
        private static bool Prefix(Container __instance, ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            InventoryActionRequest.Transfer(Player.m_localPlayer.GetInventory(), __instance.GetInventory(), null, 0, "all");
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Container), "Save")]
    internal static class ContainerClientSaveGuard { private static bool Prefix() => !ClientDamageGuard.Active; }
    [HarmonyPatch(typeof(Container), nameof(Container.SetInUse))]
    internal static class ContainerClientUseGuard { private static bool Prefix() => !ClientDamageGuard.Active; }
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Pickup))]
    internal static class PickupRequest
    {
        private static bool Prefix(ItemDrop __instance, Humanoid character)
        {
            if (!ClientDamageGuard.Active) return true;
            if (character == Player.m_localPlayer) InventoryActionRequest.Pickup(__instance);
            return false;
        }
    }
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.RequestOwn))]
    internal static class AutoPickupRequest
    {
        private static bool Prefix(ItemDrop __instance)
        {
            if (!ClientDamageGuard.Active) return true;
            InventoryActionRequest.Pickup(__instance, true);
            return false;
        }
    }
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
    internal static class DirectPickupRequest
    {
        private static bool Prefix(Humanoid __instance, GameObject go, ref bool __result)
        {
            if (!ClientDamageGuard.Active) return true;
            var drop = go == null ? null : go.GetComponent<ItemDrop>();
            if (__instance == Player.m_localPlayer && drop != null) InventoryActionRequest.Pickup(drop);
            __result = false; return false;
        }
    }
}
