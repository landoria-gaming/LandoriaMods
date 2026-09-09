using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static RavenWatch.Tools.WireSchema;

namespace RavenWatch.Tools;

internal static class BinaryLayouts
{
    internal static JsonObject Generate(string source)
    {
        string variables = File.ReadAllText(Path.Combine(source, "ZDOVars.cs"));
        string Key(string variable)
        {
            var match = Regex.Match(variables, Regex.Escape(variable) + "\\s*=\\s*\"([^\"]+)\"\\.GetStableHashCode");
            if (!match.Success) throw new InvalidDataException("Missing ZDO key " + variable);
            return RpcInventory.StableHash(match.Groups[1].Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return new()
        {
            [Key("s_roomData")] = Struct("DungeonGenerator.Load", Array("rooms", Struct("DungeonGenerator.Save",
                Field("prefabHash", "int32"), Field("position", "Vector3"), Field("rotation", "Quaternion")))),
            [Key("s_TCData")] = Terrain(), [Key("s_liquidData")] = Liquid(),
            [Key("s_items")] = Struct("Inventory.Save / Load (current wire version)", Version("int32", 109), Array("items", Item(), "uint16")),
            [Key("s_itemData")] = Struct("ItemDrop.SaveToZDO / LoadFromZDO (current wire version)", Version("uint8", 109), Named("item", Item())),
            [Key("s_data")] = new JsonObject { ["type"] = "componentSwitch", ["variants"] = new JsonObject
                { ["MapTable"] = Map(), ["ArcheryTarget"] = Struct("ArcheryTarget.ProjectileHit", Array("scores", JsonValue.Create("uint8")!, null!)) } }
        };
    }

    internal static JsonObject Map() => Compressed(Struct("Minimap.GetSharedMapData / AddSharedMapData; MapTable.GetMapData",
        Version("int32", 1, 2, 3), Array("explored", JsonValue.Create("boolean")!),
        When(Array("pins", Struct("Minimap.AddSharedMapData", Field("ownerId", "int64"), Field("name", "string"), Field("position", "Vector3"),
            Field("pinType", "int32"), Field("checked", "boolean"), When(Field("author", "string"), "gte", "$root.version", JsonValue.Create(3)!))),
            "gte", "version", JsonValue.Create(2)!)));

    private static JsonObject Terrain()
    {
        JsonObject Modified(string name) => When(Field(name, "float32"), "equals", "modified", JsonValue.Create(true)!);
        return Compressed(Struct("TerrainComp.Save / Load", Version("int32", 1), Field("operations", "int32"),
            Field("lastOperationPoint", "Vector3"), Field("lastOperationRadius", "float32"),
            Array("heights", Struct("TerrainComp.Load heights", Field("modified", "boolean"), Modified("levelDelta"), Modified("smoothDelta"))),
            Array("paint", Struct("TerrainComp.Load paint", Field("modified", "boolean"), Modified("r"), Modified("g"), Modified("b"), Modified("a")))));
    }

    private static JsonObject Liquid() => Compressed(Struct("LiquidVolume.Save / Load", Version("int32", 1, 2),
        Array("depthHundredths", JsonValue.Create("int16")!), When(Field("totalVolume", "float32"), "gte", "version", JsonValue.Create(2)!)));

    private static JsonObject Item() => Struct("ItemDrop.ItemData.Save / Load version 109", Field("durabilityHundredths", "int32"),
        Field("gridX", "uint8"), Field("gridY", "uint8"), Field("worldLevel", "uint8"),
        new JsonObject { ["name"] = "flags", ["type"] = "uint8", ["bits"] = new JsonObject { ["pickedUp"] = 1, ["equipped"] = 2 } },
        Flag("quality", "uint16", 4, JsonValue.Create(1)), Flag("stack", "uint16", 8, JsonValue.Create(1)),
        Flag("variant", "int32", 16, JsonValue.Create(0)), Flag("crafterId", "int64", 32, JsonValue.Create("0")),
        Flag("crafterName", "string", 32, JsonValue.Create("")), Flag("prefabHash", "int32", 64, JsonValue.Create(0)),
        When(Strings("customData", "numItems"), "bitSet", "flags", JsonValue.Create(128)!),
        new JsonObject { ["name"] = "extraFlags", ["type"] = "uint8", ["bits"] = new JsonObject { ["cheated"] = 1 } });
}
