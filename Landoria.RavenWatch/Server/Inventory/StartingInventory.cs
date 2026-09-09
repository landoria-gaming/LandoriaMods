using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class StartingInventory
    {
        private const int TorchHash = 795277336;
        private const int RagTunicHash = -1873790835;

        internal static JArray Create() => new JArray(
            Item("Torch", TorchHash),
            Item("ArmorRagsChest", RagTunicHash));

        internal static bool Matches(int prefabHash, ItemDrop.ItemData item)
            => (prefabHash == TorchHash || prefabHash == RagTunicHash)
                && item.m_quality == 1 && item.m_variant == 0 && item.m_worldLevel == 0;

        private static JObject Item(string prefab, int hash)
            => new JObject
            {
                ["worldLevel"] = 0,
                ["quality"] = 1, ["quantity"] = 1, ["variant"] = 0,
                ["prefabHash"] = hash, ["prefabName"] = prefab
            };
    }
}
