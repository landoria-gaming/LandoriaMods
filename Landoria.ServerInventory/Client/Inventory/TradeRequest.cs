using HarmonyLib;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class BuyRequest
    {
        private static bool Prefix(Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
        {
            if (!ClientDamageGuard.Active) return true;
            if (___m_trader == null || ___m_selectedItem == null) return false;
            var request = InventoryActionRequest.Request(___m_trader, "trade");
            if (request != null)
            {
                request["offer"] = ___m_trader.m_items.IndexOf(___m_selectedItem);
                WorldActionRequest.Send(request);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(StoreGui), "SellItem")]
    internal static class SellRequest
    {
        private static bool Prefix(Trader ___m_trader)
        {
            if (!ClientDamageGuard.Active) return true;
            if (___m_trader == null) return false;
            var request = InventoryActionRequest.Request(___m_trader, "trade");
            if (request != null) { request["sell"] = true; WorldActionRequest.Send(request); }
            return false;
        }
    }
}
