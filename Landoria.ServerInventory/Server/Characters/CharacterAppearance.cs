using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Server
{
    internal static class CharacterAppearance
    {
        internal static void Apply(JObject player, string json)
        {
            if (json == null || json.Length > 4096) throw new InvalidDataException("Invalid initial appearance size.");
            var appearance = JObject.Parse(json);
            if (appearance["modelIndex"]?.Type != JTokenType.Integer) throw new InvalidDataException("Invalid character model.");
            int model = (int)appearance["modelIndex"];
            var visuals = Game.instance.m_playerPrefab.GetComponentInChildren<VisEquipment>();
            if (visuals == null || model < 0 || model >= visuals.m_models.Length) throw new InvalidDataException("Unknown character model.");
            player["modelIndex"] = model;
            player["hairItem"] = Accessory(appearance, "hairItem", "Hair");
            player["beardItem"] = Accessory(appearance, "beardItem", "Beard");
            player["skinColor"] = Color(appearance["skinColor"]);
            player["hairColor"] = Color(appearance["hairColor"]);
        }

        private static string Accessory(JObject appearance, string key, string type)
        {
            if (appearance[key]?.Type != JTokenType.String) throw new InvalidDataException("Invalid appearance accessory.");
            string name = (string)appearance[key];
            if (name.Length > 128) throw new InvalidDataException("Invalid accessory name.");
            if (name.Length != 0 && !ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, type)
                .Any(item => item.gameObject.name == name)) throw new InvalidDataException("Unknown " + type + " accessory.");
            return name;
        }

        private static JObject Color(JToken value)
        {
            if (!(value is JObject color)) throw new InvalidDataException("Invalid appearance color.");
            var result = new JObject();
            foreach (string axis in new[] { "x", "y", "z" })
            {
                var token = color[axis];
                if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
                    throw new InvalidDataException("Invalid appearance color component.");
                float component = (float)token;
                if (float.IsNaN(component) || component < 0 || component > 1) throw new InvalidDataException("Appearance color out of range.");
                result[axis] = component;
            }
            return result;
        }
    }
}
