using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Landoria.ServerInventory.Network;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch]
    internal static class WorldInventoryActions
    {
        internal static ZNetView Target(WorldActionTransaction action, JObject request)
        {
            var id = new ZDOID(long.Parse((string)request["user"]), (uint)request["object"]);
            var view = ZNetScene.instance.FindInstance(id)?.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || Vector3.Distance(action.Player.transform.position, view.transform.position) > 4f)
                throw new InvalidOperationException("Object unavailable or too far away.");
            if (!PrivateArea.CheckAccess(view.transform.position, 0, false)) throw new InvalidOperationException("No access.");
            view.GetZDO().SetOwner(ZNet.GetUID());
            action.Touch(view);
            return view;
        }

        internal static void Pickup(WorldActionTransaction action, JObject request)
        {
            var view = Target(action, request);
            var drop = view.GetComponent<ItemDrop>();
            if (drop == null) throw new InvalidDataException("Target is not an item.");
            drop.Load();
            if (!drop.CanPickup() || drop.IsPiece() || drop.InTar() || drop.m_itemData.m_shared.m_questItem)
                throw new InvalidOperationException("Item cannot be picked up.");
            if ((bool?)request["automatic"] == true && (!drop.m_autoPickup ||
                Vector3.Distance(action.Player.transform.position + Vector3.up, drop.transform.position) > action.Player.m_autoPickupRange ||
                action.Inventory.GetTotalWeight() + drop.m_itemData.GetWeight() > action.Player.GetMaxCarryWeight()))
                throw new InvalidOperationException("Automatic pickup requirements not met.");
            var item = drop.m_itemData.Clone();
            if (!action.Inventory.CanAddItem(item, item.m_stack) || !action.Inventory.AddItem(item))
                throw new InvalidOperationException("Inventory full.");
            action.Delete(view);
        }

        internal static void Container(WorldActionTransaction action, JObject request)
        {
            var view = Target(action, request);
            var container = view.GetComponent<Container>();
            if (container == null || !Access(container, action.PlayerId)) throw new InvalidOperationException("Container access denied.");
            Load(container);
            var source = container.GetInventory();
            var copy = new Inventory(source.GetName(), null, source.GetWidth(), source.GetHeight());
            var bytes = new ZPackage(); source.Save(bytes); copy.Load(new ZPackage(bytes.GetArray()));
            string operation = (string)request["operation"];
            action.ReadOnly = operation == "open";
            if (operation != "open") Transfer(action, request, copy);
            var result = new ZPackage(); copy.Save(result);
            if (operation != "open") action.BeforeCommit(() =>
            { source.Load(new ZPackage(result.GetArray())); Save(container); });
            action.AfterCommit(() => action.Peer.m_rpc.Invoke(CharacterRpc.ContainerSnapshot,
                view.GetZDO().m_uid, operation == "open", result));
        }

        private static void Transfer(WorldActionTransaction action, JObject request, Inventory container)
        {
            bool deposit = (bool?)request["deposit"] == true;
            var source = deposit ? action.Inventory : container;
            var destination = deposit ? container : action.Inventory;
            string operation = (string)request["operation"];
            if (operation == "all") { destination.MoveAll(source); return; }
            if (operation == "stack") { destination.StackAll(source); return; }
            if (operation != "item") throw new InvalidDataException("Invalid transfer operation.");
            var item = source.GetItemAt((int)request["x"], (int)request["y"]);
            int amount = (int)request["amount"];
            if (item == null || item.m_dropPrefab.name.GetStableHashCode() != (int)request["prefab"] ||
                amount < 1 || amount > item.m_stack || item.m_shared.m_questItem)
                throw new InvalidOperationException("Item changed or cannot be transferred.");
            var clone = item.Clone(); clone.m_stack = amount; clone.m_equipped = false;
            if (!destination.CanAddItem(clone, amount) || !destination.AddItem(clone)) throw new InvalidOperationException("Inventory full.");
            source.RemoveItem(item, amount);
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(Container), "CheckAccess")]
        private static bool Access(Container instance, long playerID) => throw new NotImplementedException();
        [HarmonyReversePatch, HarmonyPatch(typeof(Container), "Save")]
        private static void Save(Container instance) => throw new NotImplementedException();
        [HarmonyReversePatch, HarmonyPatch(typeof(Container), "Load")]
        private static bool Load(Container instance) => throw new NotImplementedException();
    }
}
