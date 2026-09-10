using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Landoria.RavenWatchTool;

internal static class RpcInventory
{
    private static readonly Regex Register = new("(?<receiver>[\\w.]+)\\.Register\\s*(?:<(?<types>.*?)>)?\\s*\\(\\s*(?<name>\"[^\"]+\"|\\w+)\\s*,\\s*(?<handler>\\w+)\\s*\\)", RegexOptions.Singleline);
    private static readonly Regex Send = new(@"(?<receiver>[\w.]+)(?:\?)?\.(?<api>InvokeRPC|InvokeRoutedRPC|Invoke)\s*\((?<args>[^;]*?)\)\s*;", RegexOptions.Singleline);

    internal static IEnumerable<JsonObject> Extract(SourceFile file, string side)
    {
        foreach (Match match in Register.Matches(file.Text))
        {
            var types = SourceFile.Split(match.Groups["types"].Value);
            var candidates = file.Methods.Where(m => m.Name == match.Groups["handler"].Value
                && SourceFile.Split(m.Signature).Count == types.Count + 1).ToList();
            if (candidates.Count != 1) throw new InvalidDataException("Unresolved handler: " + file.Name + " " + match.Value);
            string context = SourceFile.Split(candidates[0].Signature)[0];
            if (!context.StartsWith("ZRpc ") && !context.StartsWith("long ")) continue;
            yield return Record(file, side, match, types, candidates[0]);
        }
    }

    private static JsonObject Record(SourceFile file, string side, Match match, List<string> types, SourceMethod method)
    {
        string name = ResolveName(file, match.Groups["name"].Value), receiver = match.Groups["receiver"].Value;
        var parameters = SourceFile.Split(method.Signature);
        JsonArray args = [];
        for (int i = 0; i < types.Count; i++)
            args.Add(new JsonObject { ["index"] = i, ["name"] = parameters[i + 1].Split('=')[0].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[^1], ["type"] = types[i] });
        var (responses, traces) = Outbound(file, method);
        return new() { ["name"] = name, ["hash"] = StableHash(name), ["side"] = side,
            ["transport"] = receiver.Contains("nview", StringComparison.OrdinalIgnoreCase) ? "object" : receiver.Contains("ZRoutedRpc") ? "routed" : "direct",
            ["source"] = file.Name, ["registrationLine"] = file.Line(match.Index), ["handler"] = method.Name,
            ["handlerContext"] = parameters[0], ["parameters"] = args, ["returnType"] = method.ReturnType,
            ["synchronousResponse"] = false, ["outboundCandidates"] = responses, ["wireReaderTraces"] = traces };
    }

    private static string ResolveName(SourceFile file, string expression)
    {
        if (expression.StartsWith('"')) return expression[1..^1];
        var constant = Regex.Match(file.Text, @"\bstring\s+" + Regex.Escape(expression) + "\\s*=\\s*\"([^\"]+)\"");
        if (!constant.Success) throw new InvalidDataException("Unresolved RPC name: " + file.Name + " " + expression);
        return constant.Groups[1].Value;
    }

    private static (JsonArray, JsonArray) Outbound(SourceFile file, SourceMethod method)
    {
        JsonArray responses = [], traces = [];
        foreach (var (current, chain) in file.Related(method))
        {
            var details = file.Trace(current);
            if (details["reads"]!.AsArray().Count != 0 || details["writes"]!.AsArray().Count != 0) traces.Add(details.DeepClone());
            foreach (Match send in Send.Matches(file.Body(current)))
            {
                var args = SourceFile.Split(send.Groups["args"].Value);
                string? name = args.FirstOrDefault(a => a.StartsWith('"') && a.EndsWith('"'));
                string api = send.Groups["api"].Value, receiver = send.Groups["receiver"].Value;
                if (name == null || (api == "Invoke" && !receiver.Contains("rpc", StringComparison.OrdinalIgnoreCase))) continue;
                responses.Add(new JsonObject { ["name"] = name[1..^1], ["api"] = api, ["receiver"] = receiver,
                    ["arguments"] = JsonSerializer.SerializeToNode(args), ["source"] = file.Name,
                    ["method"] = current.Name, ["callPath"] = JsonSerializer.SerializeToNode(chain),
                    ["classification"] = "outbound_candidate_not_guaranteed_response",
                    ["methodControlFlow"] = details["controlFlow"]!.DeepClone() });
            }
        }
        return (responses, traces);
    }

    internal static int StableHash(string value)
    {
        unchecked
        {
            int a = 5381, b = 5381;
            for (int i = 0; i < value.Length; i++)
                if (i % 2 == 0) a = a * 33 ^ value[i];
                else b = b * 33 ^ value[i];
            return a + b * 1566083941;
        }
    }
}
