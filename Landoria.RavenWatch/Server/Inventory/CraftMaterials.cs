using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class CraftMaterials
    {
        internal static void Add(JObject entry)
        {
            var recipes = new JArray();
            entry["requiredMaterials"] = new JObject
            {
                ["source"] = "server_recipes", ["craftCountSource"] = "client_reported",
                ["meaning"] = "Recipe requirements, not proof of materials consumed.",
                ["status"] = ObjectDB.instance == null ? "database_unavailable" : "recipe_not_found",
                ["recipes"] = recipes
            };
            if (ObjectDB.instance == null) return;
            var crafted = (JObject)entry["craftedItem"];
            int hash = (int)crafted["prefabHash"], quality = (int)crafted["quality"];
            bool upgrade = (bool)entry["upgrade"], upgrader = (bool)entry["upgrader"];
            int count = (int)entry["craftCount"];
            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe == null || !recipe.m_enabled || recipe.m_item == null
                    || recipe.m_item.gameObject.name.GetStableHashCode() != hash
                    || (!upgrade && recipe.m_noCraftOnlyUpgrade)) continue;
                recipes.Add(Describe(recipe, quality, count, upgrader));
            }
            entry["requiredMaterials"]["status"] = recipes.Count == 0 ? "recipe_not_found"
                : recipes.Count == 1 ? "resolved" : "multiple_recipes";
        }

        private static JObject Describe(Recipe recipe, int quality, int count, bool upgrader)
        {
            var materials = new JArray();
            foreach (var requirement in recipe.m_resources.Where(value => value.m_resItem != null
                && value.m_upgraderResource == upgrader))
            {
                int amount = requirement.GetAmount(quality);
                if (amount <= 0) continue;
                string prefab = requirement.m_resItem.gameObject.name;
                materials.Add(new JObject
                {
                    ["prefabHash"] = prefab.GetStableHashCode(), ["prefabName"] = prefab,
                    ["quantityPerCraft"] = amount, ["quantity"] = (long)amount * count
                });
            }
            return new JObject
            {
                ["recipe"] = recipe.name, ["quality"] = quality, ["craftCount"] = count,
                ["requirementMode"] = recipe.m_requireOnlyOneIngredient ? "any_one" : "all",
                ["materials"] = materials
            };
        }
    }
}
