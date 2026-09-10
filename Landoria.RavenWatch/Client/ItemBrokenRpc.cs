using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Client
{
    internal static class ItemBrokenRpc
    {
        internal static void Send(Player player, ItemDrop.ItemData item)
        {
            bool destroyed = !player.GetInventory().ContainsItem(item);
            var broken = InventoryEventSender.Item(item, item.m_stack);
            broken["durability"] = item.m_durability;
            broken["equipped"] = item.m_equipped;
            var changes = new JArray();
            if (destroyed)
            {
                var delta = (JObject)broken.DeepClone();
                delta["quantityDelta"] = -item.m_stack;
                changes.Add(delta);
            }
            InventoryEventSender.Send(player, new JObject { ["context"] = "item_broken",
                ["brokenItem"] = broken, ["destroyed"] = destroyed, ["changes"] = changes });
        }
    }
}
