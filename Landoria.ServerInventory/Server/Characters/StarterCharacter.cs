using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Server
{
    internal static class StarterCharacter
    {
        internal static JObject PlayerData()
        {
            var player = new JObject
            {
                ["version"] = 33, ["maxHealth"] = 25f, ["health"] = 25f,
                ["maxStamina"] = 75f, ["stamina"] = 75f, ["timeSinceDeath"] = 0f,
                ["guardianPower"] = "", ["guardianPowerCooldown"] = 0f,
                ["beardItem"] = "", ["hairItem"] = "", ["modelIndex"] = 0,
                ["skinColor"] = Color(), ["hairColor"] = Color(),
                ["maxEitr"] = 0f, ["eitr"] = 0f, ["buildUi"] = "",
                ["skills"] = new JObject { ["version"] = 2, ["values"] = new JArray() },
                ["inventory"] = new JObject
                {
                    ["version"] = 109,
                    ["items"] = new JArray(Item("Torch", 0), Item("Hammer", 1), Item("AxeStone", 2))
                }
            };
            foreach (string name in new[] { "knownRecipes", "knownStations", "knownMaterial", "shownTutorials",
                "uniques", "trophies", "knownBiome", "knownTexts", "foods", "customData" })
                player[name] = new JArray();
            return player;
        }

        private static JObject Item(string name, int slot)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(name);
            var drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            if (drop == null) throw new InvalidDataException("Missing starter item: " + name);
            return new JObject
            {
                ["durabilityHundredths"] = checked((int)(drop.m_itemData.GetMaxDurability(1) * 100f)),
                ["slotX"] = slot, ["slotY"] = 0, ["worldLevel"] = Game.m_worldLevel,
                ["flags"] = 64, ["prefabHash"] = name.GetStableHashCode(), ["extraFlags"] = 0
            };
        }

        private static JObject Color() => new JObject { ["x"] = 1f, ["y"] = 1f, ["z"] = 1f };
    }
}
