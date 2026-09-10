using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Landoria.RavenWatchTool.WireSchema;

namespace Landoria.RavenWatchTool;

// Reviewed against the dedicated-server 1.0.7 serialization code. No previous JSON is read.
internal static class WireLayouts
{
    internal static JsonObject Generate(string source)
    {
        string versionSource = File.ReadAllText(Path.Combine(source, "Version.cs"));
        if (!Regex.IsMatch(versionSource, @"CurrentVersion\s*\{[^}]+\}\s*=\s*new GameVersion\(1,\s*0,\s*7\)"))
            throw new InvalidDataException("These binary rules require Valheim 1.0.7 sources.");
        var packages = Packages();
        return new() { ["gameVersion"] = "1.0.7", ["wireFormat"] = new JsonObject
            { ["endianness"] = "little", ["stringEncoding"] = "UTF-8 with BinaryWriter 7-bit byte-length prefix",
                ["types"] = Types(), ["packages"] = packages, ["zdoBinaryValues"] = BinaryLayouts.Generate(source) },
            ["coverage"] = new JsonObject { ["packageFormats"] = new JsonArray(packages.Select(p => (JsonNode)JsonValue.Create(p.Key)!).ToArray()),
                ["limitation"] = "Unknown mod RPCs and unknown ZDO binary keys produce explicit decoding errors. Authentication ticket bytes are numeric octets; cryptographic contents are not interpreted." },
            ["responseSemantics"] = new JsonObject { ["correlation"] = "Outbound candidates are conditional calls, not guaranteed replies." } };
    }

    private static JsonObject Types() => new()
    {
        ["Vector3"] = Struct("ZPackage.Write(Vector3)", Field("x", "float32"), Field("y", "float32"), Field("z", "float32")),
        ["Quaternion"] = Struct("ZPackage.Write(Quaternion)", Field("x", "float32"), Field("y", "float32"), Field("z", "float32"), Field("w", "float32")),
        ["ZDOID"] = Struct("ZPackage.ReadZDOID", Field("userId", "int64"), Field("id", "uint32")),
        ["UserInfo"] = Struct("UserInfo.Deserialize", Field("name", "string"), Field("userId", "string")),
        ["stringList"] = Array("values", JsonValue.Create("string")!),
        ["SimulationDistance"] = Struct("SimulationDistance.Deserialize", Field("near", "int32"), Field("far", "int32"), Field("classic", "boolean")),
        ["HitData"] = Hit(), ["ZDOState"] = Zdo()
    };

    private static JsonObject Hit()
    {
        List<JsonObject> fields = [Field("serializeFlags", "uint32")];
        string[] damage = ["generic", "blunt", "slash", "pierce", "chop", "pickaxe", "fire", "frost", "lightning", "poison", "spirit"];
        for (int i = 0; i < damage.Length; i++) fields.Add(Flag("damage_" + damage[i], "float32", 1 << i, JsonValue.Create(0), "serializeFlags"));
        fields.AddRange([Flag("damage_nonPlayer", "float32", 65536, JsonValue.Create(0), "serializeFlags"), Field("toolTier", "int16"),
            Flag("pushForce", "float32", 2048, JsonValue.Create(0), "serializeFlags"),
            Flag("backstabBonus", "float32", 4096, JsonValue.Create(1), "serializeFlags"),
            Flag("staggerMultiplier", "float32", 8192, JsonValue.Create(1), "serializeFlags"),
            new JsonObject { ["name"] = "flags", ["type"] = "uint8", ["bits"] = new JsonObject { ["dodgeable"] = 1, ["blockable"] = 2, ["ranged"] = 4, ["ignorePVP"] = 8 } },
            Field("point", "Vector3"), Field("direction", "Vector3"), Field("statusEffectHash", "int32"),
            Flag("attacker", "ZDOID", 16384, new JsonObject { ["userId"] = "0", ["id"] = 0 }, "serializeFlags"),
            Field("skill", "int16"), Flag("skillRaiseAmount", "float32", 32768, JsonValue.Create(1), "serializeFlags"),
            Field("weakSpot", "char"), Field("skillLevel", "float32"), Field("itemLevel", "int16"), Field("itemWorldLevel", "uint8"),
            Field("hitType", "uint8"), Field("healthReturn", "float32"), Field("eitrAdd", "float32"), Field("radius", "float32"), Field("variant", "int16")]);
        return Struct("HitData.Deserialize / HitDefaults.SerializeFlags", fields.ToArray());
    }

    private static JsonObject Zdo()
    {
        List<JsonObject> fields = [new JsonObject { ["name"] = "flags", ["type"] = "uint16",
            ["bits"] = new JsonObject { ["persistent"] = 256, ["distant"] = 512, ["hasRotation"] = 4096 },
            ["derived"] = new JsonObject { ["objectType"] = new JsonObject { ["shift"] = 10, ["mask"] = 3 } } },
            Field("prefabHash", "int32"), Flag("rotation", "Vector3", 4096),
            When(Named("connection", Struct("ZDO.Deserialize", Field("connectionType", "uint8"), Field("target", "ZDOID"))), "bitSet", "flags", JsonValue.Create(1)!)];
        string[] types = ["float32", "Vector3", "Quaternion", "int32", "int64", "string", "bytes"];
        string[] names = ["floats", "vectors", "rotations", "integers", "longs", "strings", "byteArrays"];
        for (int i = 0; i < types.Length; i++) fields.Add(When(Array(names[i],
            Struct("ZDODataHelper.ReadData", Field("keyHash", "int32"), Field("value", types[i])), "numItems"), "bitSet", "flags", JsonValue.Create(2 << i)!));
        return Struct("ZDO.Deserialize; ZDODataHelper.ReadData; ZPackage.ReadNumItems", fields.ToArray());
    }

    private static JsonObject Packages() => new()
    {
        ["RPC_DamageText"] = Struct("DamageText.RPC_DamageText", Field("textType", "int32"), Field("position", "Vector3"), Field("text", "string"), Field("player", "boolean")),
        ["RPC_ApplyOperation"] = Struct("TerrainComp.RPC_ApplyOperation; TerrainOp.Settings.Deserialize", Field("position", "Vector3"),
            Field("hasRotation", "boolean"), When(Field("forward", "Vector3"), "equals", "hasRotation", JsonValue.Create(true)!), Field("terrainPrefabHash", "int32")),
        ["DestroyZDO"] = Struct("ZDOMan.RPC_DestroyZDO", Array("ids", JsonValue.Create("ZDOID")!)),
        ["LocationIcons"] = Struct("ZoneSystem.RPC_LocationIcons", Array("icons", Struct("ZoneSystem.RPC_LocationIcons", Field("position", "Vector3"), Field("name", "string")))),
        ["AdminList"] = Struct("ZNet.RPC_AdminList", Array("admins", JsonValue.Create("string")!)),
        ["ServerSyncedPlayerData"] = Struct("ZNet.RPC_ServerSyncedPlayerData", Field("referencePosition", "Vector3"), Field("publicPosition", "boolean"), Strings("data")),
        ["PlayerList"] = Players(), ["HistoricalPlayerList"] = History(), ["PeerInfo"] = Peer(),
        ["RPC_RequestValidSimulationDistance"] = Struct("ZNet.RPC_RequestValidSimulationDistance", Field("distance", "SimulationDistance")),
        ["RPC_ValidatedSimulationDistance"] = Struct("ZNet.RPC_ValidatedSimulationDistance", Field("distance", "SimulationDistance")),
        ["ZDOData"] = ZdoData(), ["RoutedRPC"] = Routed(), ["MapData"] = BinaryLayouts.Map()
    };

    private static JsonObject Peer()
    {
        JsonObject[] Common() => [Field("sessionId", "int64"), Field("version", "string"), Field("networkVersion", "uint32"),
            Field("referencePosition", "Vector3"), Field("playerName", "string"), Field("playFabId", "string"), Field("simulationDistance", "SimulationDistance")];
        return new() { ["type"] = "directionSwitch", ["variants"] = new JsonObject
        {
            ["client_to_server"] = Struct("ZNet.SendPeerInfo client branch", [.. Common(), Field("passwordHash", "string"), Field("inviteSecretKey", "string"), Field("sessionTicket", "bytes")]),
            ["server_to_client"] = Struct("ZNet.SendPeerInfo server branch", [.. Common(), Field("worldName", "string"), Field("seed", "int32"),
                Field("seedName", "string"), Field("worldId", "int64"), Field("worldGenVersion", "int32"), Field("netTime", "float64")])
        } };
    }

    private static JsonObject[] UserFields() => [Field("userId", "string"), Field("displayName", "string"),
        Field("serverAssignedDisplayName", "string"), Field("playFabId", "string")];
    private static JsonObject Players() => Struct("ZNet.RPC_PlayerList", Array("players", Struct("ZNet.WritePlayerInfo",
        [Field("name", "string"), Field("characterId", "ZDOID"), .. UserFields(), Field("publicPosition", "boolean"),
            When(Field("position", "Vector3"), "equals", "publicPosition", JsonValue.Create(true)!)])));
    private static JsonObject History() => Struct("ZNet.RPC_HistoricalPlayerList", Array("players", Struct("CrossNetworkUserInfo", UserFields())));

    private static JsonObject ZdoData() => Struct("ZDOMan.RPC_ZDOData", Array("invalidatedSectors", JsonValue.Create("ZDOID")!),
        new JsonObject { ["name"] = "objects", ["type"] = "repeatUntil", ["prefix"] = new JsonArray(Field("id", "ZDOID")),
            ["stopWhen"] = new JsonObject { ["op"] = "equals", ["field"] = "id", ["value"] = new JsonObject { ["userId"] = "0", ["id"] = 0 } },
            ["fields"] = new JsonArray(Field("ownerRevision", "uint16"), Field("dataRevision", "uint32"), Field("owner", "int64"), Field("position", "Vector3"),
                new JsonObject { ["name"] = "state", ["type"] = "package", ["schema"] = "ZDOState", ["byteLengthPrefix"] = "int32" }) });

    private static JsonObject Routed() => Struct("ZRoutedRpc.RoutedRPCData.Deserialize", Field("messageId", "int64"), Field("senderPeerId", "int64"),
        Field("targetPeerId", "int64"), Field("targetZdo", "ZDOID"), Field("methodHash", "int32"),
        new JsonObject { ["name"] = "parameters", ["type"] = "package", ["byteLengthPrefix"] = "int32", ["schemaDispatch"] = "targetZdo_and_methodHash" });
}
