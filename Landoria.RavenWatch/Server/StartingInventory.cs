using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class StartingInventory
    {
        internal static JArray Create() => new JArray(
            Item("Torch", 795277336, 20, 0, false),
            Item("ArmorRagsChest", -1873790835, 200, 3, true));

        private static JObject Item(string prefab, int hash, int durability, int gridY, bool equipped)
            => new JObject
            {
                ["durability"] = durability, ["gridX"] = 0, ["gridY"] = gridY,
                ["worldLevel"] = 0, ["pickedUp"] = false, ["equipped"] = equipped,
                ["quality"] = 1, ["quantity"] = 1, ["variant"] = 0,
                ["crafterId"] = "0", ["crafterName"] = "",
                ["prefabHash"] = hash, ["prefabName"] = prefab,
                ["customData"] = new JArray(), ["cheated"] = false
            };
    }
}
