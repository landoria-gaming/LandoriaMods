using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.ServerInventory.Server
{
    [HarmonyPatch]
    internal static class ServerCrafting
    {
        [ThreadStatic] internal static CraftingStation Station;
        internal static void Craft(WorldActionTransaction action, JObject request)
        {
            int hash = (int)request["prefab"];
            var recipes = ObjectDB.instance.m_recipes.Where(r => r.m_enabled && r.m_item != null &&
                r.m_item.gameObject.name.GetStableHashCode() == hash).ToArray();
            if (recipes.Length != 1) throw new InvalidOperationException("Unknown or ambiguous recipe.");
            var recipe = recipes[0];
            var upgrade = Upgrade(action.Inventory, request, hash);
            int quality = upgrade == null ? 1 : upgrade.m_quality + 1;
            int count = (int?)request["count"] ?? 1, variant = (int?)request["variant"] ?? 0;
            if (count < 1 || count > 100 || (upgrade != null && count != 1) ||
                quality > recipe.m_item.m_itemData.m_shared.m_maxQuality ||
                (upgrade == null && recipe.m_noCraftOnlyUpgrade) || variant < 0 ||
                variant >= recipe.m_item.m_itemData.m_shared.m_icons.Length)
                throw new InvalidOperationException("Unsupported craft quantity, quality or variant.");
            Station = FindStation(action, recipe, quality);
            try
            {
                if (!action.Player.HaveRequirements(recipe, false, quality, count)) throw new InvalidOperationException("Missing craft requirements.");
                Create(action, recipe, upgrade, quality, count, variant);
            }
            finally { Station = null; }
        }

        private static ItemDrop.ItemData Upgrade(Inventory inventory, JObject request, int hash)
        {
            if ((bool?)request["upgrade"] != true) return null;
            var item = inventory.GetItemAt((int)request["x"], (int)request["y"]);
            if (item == null || item.m_dropPrefab.name.GetStableHashCode() != hash || item.m_quality != (int)request["quality"])
                throw new InvalidOperationException("Upgrade item changed or is missing.");
            return item;
        }

        private static CraftingStation FindStation(WorldActionTransaction action, Recipe recipe, int quality)
        {
            var required = recipe.GetRequiredStation(quality);
            if (required == null) return null;
            var station = UnityEngine.Object.FindObjectsByType<CraftingStation>(FindObjectsSortMode.None)
                .Where(s => s.m_name == required.m_name && !s.m_upgrader && s.InUseDistance(action.Player) &&
                    s.GetLevel() >= recipe.GetRequiredStationLevel(quality) && s.CheckUsable(action.Player, false))
                .OrderBy(s => Vector3.Distance(s.transform.position, action.Player.transform.position)).FirstOrDefault();
            if (station == null || !PrivateArea.CheckAccess(station.transform.position, 0, false))
                throw new InvalidOperationException("Required crafting station unavailable.");
            return station;
        }

        private static void Create(WorldActionTransaction action, Recipe recipe, ItemDrop.ItemData upgrade, int quality, int count, int variant)
        {
            ItemDrop.ItemData ingredient = null;
            int needed = 0, amount;
            if (recipe.m_requireOnlyOneIngredient)
            {
                ingredient = action.Player.GetFirstRequiredItem(action.Inventory, recipe, quality, out needed, out int extra, count);
                if (ingredient == null) throw new InvalidOperationException("Missing craft ingredient.");
                amount = checked((recipe.m_amount + (int)Mathf.Ceil((ingredient.m_quality - 1) * recipe.m_amount * recipe.m_qualityResultAmountMultiplier) + extra) * count);
            }
            else amount = recipe.GetAmount(quality, out _, out _, count);
            if (amount < 1) throw new InvalidDataException("Invalid recipe output.");
            bool cheated = action.Inventory.ItemCheated(recipe.m_resources);
            var position = upgrade == null ? new Vector2i(-1, -1) : upgrade.m_gridPos;
            if (upgrade != null) { variant = upgrade.m_variant; action.Inventory.RemoveItem(upgrade); }
            if (!action.Inventory.CanAddItem(recipe.m_item.gameObject, amount)) throw new InvalidOperationException("Inventory full.");
            var created = action.Inventory.AddItem(recipe.m_item.gameObject.name, amount, quality, variant,
                action.PlayerId, action.PlayerName, position, cheated);
            if (created == null) throw new InvalidOperationException("Could not create crafted item.");
            if (ingredient != null) action.Inventory.RemoveItem(ingredient.m_shared.m_name, needed, ingredient.m_quality);
            else action.Player.ConsumeResources(recipe.m_resources, quality, -1, count);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RequiredCraftingStation))]
    internal static class CraftStationContext
    {
        private static void Prefix(ref CraftingStation ___m_currentStation, out CraftingStation __state)
        {
            __state = ___m_currentStation;
            if (WorldActionTransaction.Current != null) ___m_currentStation = ServerCrafting.Station;
        }
        private static Exception Finalizer(ref CraftingStation ___m_currentStation, CraftingStation __state, Exception __exception)
        { ___m_currentStation = __state; return __exception; }
    }
    [HarmonyPatch(typeof(Player), nameof(Player.GetCurrentCraftingStation))]
    internal static class CraftStationQuery
    {
        private static bool Prefix(ref CraftingStation __result)
        {
            if (WorldActionTransaction.Current == null) return true;
            __result = ServerCrafting.Station; return false;
        }
    }
}
