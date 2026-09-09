using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class StartingInventory
    {
        internal static JArray Create() => new JArray(
            Item("Torch", 795277336),
            Item("ArmorRagsChest", -1873790835));

        private static JObject Item(string prefab, int hash)
            => new JObject
            {
                ["worldLevel"] = 0,
                ["quality"] = 1, ["quantity"] = 1, ["variant"] = 0,
                ["prefabHash"] = hash, ["prefabName"] = prefab
            };
    }
}
