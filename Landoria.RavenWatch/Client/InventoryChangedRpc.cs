using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Client
{
    internal static class InventoryChangedRpc
    {
        internal static void Send(Player player, string operation, ZPackage before)
        {
            var changes = InventoryEventSender.Changes(player, before);
            if (changes.Count == 0) return;
            bool added = changes.Any(item => (long)item["quantityDelta"] > 0);
            bool removed = changes.Any(item => (long)item["quantityDelta"] < 0);
            InventoryEventSender.Send(player, new JObject
            {
                ["context"] = added && removed ? "items_changed" : added ? "items_entered" : "items_exited",
                ["operation"] = operation, ["changes"] = changes
            });
        }
    }
}
