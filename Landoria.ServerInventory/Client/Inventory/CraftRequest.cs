using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Client
{
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class CraftRequest
    {
        private static bool Prefix(Recipe ___m_craftRecipe, ItemDrop.ItemData ___m_craftUpgradeItem,
            bool ___m_multiCrafting, int ___m_multiCraftAmount, int ___m_craftVariant)
        {
            if (!ClientDamageGuard.Active) return true;
            if (___m_craftRecipe == null) return false;
            var request = new JObject { ["kind"] = "craft", ["prefab"] = ___m_craftRecipe.m_item.gameObject.name.GetStableHashCode(),
                ["count"] = ___m_multiCrafting ? ___m_multiCraftAmount : 1, ["variant"] = ___m_craftVariant,
                ["upgrade"] = ___m_craftUpgradeItem != null };
            if (___m_craftUpgradeItem != null)
            {
                request["x"] = ___m_craftUpgradeItem.m_gridPos.x; request["y"] = ___m_craftUpgradeItem.m_gridPos.y;
                request["quality"] = ___m_craftUpgradeItem.m_quality;
            }
            WorldActionRequest.Send(request);
            return false;
        }
    }
}
