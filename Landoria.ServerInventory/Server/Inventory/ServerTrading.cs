using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerTrading
    {
        internal static void Trade(WorldActionTransaction action, JObject request)
        {
            var trader = WorldInventoryActions.Target(action, request).GetComponent<Trader>();
            if (trader == null) throw new InvalidOperationException("Trader unavailable.");
            var prefab = ObjectDB.instance.GetItemPrefab("Coins");
            var coin = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
            coin.m_dropPrefab = prefab;
            if ((bool?)request["sell"] == true) { Sell(action, coin); return; }
            int index = (int)request["offer"];
            if (index < 0 || index >= trader.m_items.Count) throw new InvalidOperationException("Unknown trade offer.");
            var offer = trader.m_items[index];
            if (offer.m_prefab == null || !string.IsNullOrEmpty(offer.m_buyKey) || !string.IsNullOrEmpty(offer.m_incrementKey))
                throw new InvalidOperationException("Only item purchases are currently supported.");
            if ((!string.IsNullOrEmpty(offer.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(offer.m_requiredGlobalKey)) ||
                offer.m_price < 0 || action.Inventory.CountItems(coin.m_shared.m_name) < offer.m_price)
                throw new InvalidOperationException("Trade requirements not met.");
            var item = offer.m_prefab.m_itemData.Clone();
            item.m_stack = Mathf.Min(offer.m_stack, item.m_shared.m_maxStackSize);
            item.m_dropPrefab = offer.m_prefab.gameObject;
            if (item.m_stack < 1 || !action.Inventory.CanAddItem(item, item.m_stack) || !action.Inventory.AddItem(item))
                throw new InvalidOperationException("Inventory full.");
            action.Inventory.RemoveItem(coin.m_shared.m_name, offer.m_price);
        }
        private static void Sell(WorldActionTransaction action, ItemDrop.ItemData coin)
        {
            var valuables = new List<ItemDrop.ItemData>();
            action.Inventory.GetValuableItems(valuables);
            var item = valuables.FirstOrDefault(i => i.m_shared.m_name != coin.m_shared.m_name);
            if (item == null || item.m_shared.m_questItem) throw new InvalidOperationException("Nothing to sell.");
            int amount = checked(item.m_stack * item.m_shared.m_value);
            if (amount < 1) throw new InvalidOperationException("Item has no sale value.");
            action.Inventory.RemoveItem(item);
            if (!action.Inventory.CanAddItem(coin.m_dropPrefab, amount) ||
                action.Inventory.AddItem(coin.m_dropPrefab.name, amount, coin.m_quality, coin.m_variant, 0, "", false) == null)
                throw new InvalidOperationException("No room for coins.");
        }
    }
}
