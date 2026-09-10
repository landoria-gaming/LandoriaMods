using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Client
{
    internal static class ItemCraftedRpc
    {
        internal static void Send(Player player, ItemDrop.ItemData item, int quantity, bool upgrade,
            int craftCount, bool upgrader)
        {
            InventoryEventSender.Send(player, new JObject { ["context"] = "item_crafted",
                ["craftedItem"] = InventoryEventSender.Item(item, quantity), ["upgrade"] = upgrade,
                ["craftCount"] = craftCount, ["upgrader"] = upgrader });
        }
    }
}
