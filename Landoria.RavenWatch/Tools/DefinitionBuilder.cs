using System.Text.Json;
using System.Text.Json.Nodes;

namespace RavenWatch.Tools;

internal static class DefinitionBuilder
{
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["int"] = "int32", ["uint"] = "uint32", ["long"] = "int64", ["float"] = "float32",
        ["double"] = "float64", ["bool"] = "boolean", ["string"] = "string", ["Vector3"] = "Vector3",
        ["Quaternion"] = "Quaternion", ["ZDOID"] = "ZDOID", ["HitData"] = "HitData", ["UserInfo"] = "UserInfo",
        ["List<string>"] = "stringList"
    };

    internal static JsonObject Generate(string source, JsonObject reviewed)
    {
        List<JsonObject> records = [];
        JsonArray serializers = [];
        const string side = "dedicated server";
        foreach (string path in Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories)
            .Where(p => !Path.GetRelativePath(source, p).Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            var file = new SourceFile(path, source);
            records.AddRange(RpcInventory.Extract(file, side));
            foreach (var method in file.Methods)
            {
                if (!new[] { "Serialize", "Deserialize", "Save", "Load", "GetMapData", "SetMapData" }.Contains(method.Name)) continue;
                var details = file.Trace(method);
                if (details["reads"]!.AsArray().Count == 0 && details["writes"]!.AsArray().Count == 0) continue;
                details["side"] = side;
                serializers.Add(details);
            }
        }
        AttachLayouts(records, reviewed);
        return Document(records, serializers, reviewed);
    }

    private static void AttachLayouts(List<JsonObject> records, JsonObject reviewed)
    {
        var packages = reviewed["wireFormat"]!["packages"]!.AsObject();
        for (int i = 0; i < records.Count; i++)
        {
            var rpc = records[i];
            rpc["id"] = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            JsonArray fields = [];
            foreach (var parameter in rpc["parameters"]!.AsArray())
                fields.Add(Parameter(parameter!, (string)rpc["name"]!, packages));
            rpc["wireSchema"] = new JsonObject { ["type"] = "struct", ["fields"] = fields,
                ["source"] = (string)rpc["source"]! + ":" + rpc["registrationLine"], ["requireEndOfStream"] = true };
            rpc["wireSchemaStatus"] = "specified";
        }
        foreach (var rpc in records)
            foreach (var response in rpc["outboundCandidates"]!.AsArray())
                response!["definitionIds"] = JsonSerializer.SerializeToNode(records
                    .Where(r => (string?)r["name"] == (string?)response["name"]).Select(r => (string)r["id"]!).ToArray());
    }

    private static JsonObject Parameter(JsonNode parameter, string rpcName, JsonObject packages)
    {
        string type = (string)parameter["type"]!;
        if (type != "ZPackage") return new() { ["name"] = (string)parameter["name"]!, ["type"] = Aliases[type] };
        if (!packages.ContainsKey(rpcName)) throw new InvalidDataException("Missing package layout: " + rpcName);
        return new() { ["name"] = (string)parameter["name"]!, ["type"] = "package", ["byteLengthPrefix"] = "int32",
            ["schema"] = rpcName, ["registry"] = "packages" };
    }

    private static JsonObject Document(List<JsonObject> records, JsonArray serializers, JsonObject reviewed)
    {
        JsonArray sorted = [];
        foreach (var record in records.OrderBy(r => (string)r["name"]!, StringComparer.Ordinal)
            .ThenBy(r => (string)r["side"]!, StringComparer.Ordinal).ThenBy(r => (string)r["source"]!, StringComparer.Ordinal)) sorted.Add(record);
        return new() { ["schemaVersion"] = 2, ["gameVersion"] = reviewed["gameVersion"]!.DeepClone(),
            ["purpose"] = "Declarative binary RPC layouts plus source-derived evidence and conditional response candidates.",
            ["semantics"] = new JsonArray("Void RPCs have no implicit response.",
                "Outbound candidates are same-class reachable calls, not guaranteed replies; runtime conditions and routing apply.",
                "Cross-class callbacks, dynamic names and mod registrations need separate analysis.",
                "Handler context is supplied by transport, not serialized.",
                "Source traces are supporting evidence; wireFormat and wireSchema are the decoder definitions."),
            ["rpcCount"] = records.Count, ["uniqueNames"] = records.Select(r => (string)r["name"]!).Distinct().Count(),
            ["rpcs"] = sorted, ["serializers"] = serializers, ["unresolvedRegistrations"] = new JsonArray(),
            ["wireFormat"] = reviewed["wireFormat"]!.DeepClone(),
            ["coverage"] = reviewed["coverage"]!.DeepClone(), ["responseSemantics"] = reviewed["responseSemantics"]!.DeepClone() };
    }
}
