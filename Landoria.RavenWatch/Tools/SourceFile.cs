using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RavenWatch.Tools;

internal sealed record SourceMethod(string Name, string ReturnType, string Signature, int Start, int End, int Line);

internal sealed class SourceFile
{
    private static readonly Regex MethodsPattern = new(@"^\s*(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|sealed|async|new)\s+)*(?<return>[\w.<>\[\],]+)\s+(?<name>\w+)\s*\((?<args>[^;{}]*?)\)\s*\{", RegexOptions.Multiline);
    internal string Name { get; }
    internal string Text { get; }
    internal List<SourceMethod> Methods { get; } = [];

    internal SourceFile(string path, string root)
    {
        Name = Path.GetRelativePath(root, path).Replace('\\', '/');
        Text = File.ReadAllText(path).Replace("\r\n", "\n");
        foreach (Match match in MethodsPattern.Matches(Text))
        {
            int start = match.Index + match.Length - 1;
            Methods.Add(new(match.Groups["name"].Value, match.Groups["return"].Value,
                match.Groups["args"].Value.Trim(), start, BlockEnd(start), Line(match.Index)));
        }
    }

    internal int Line(int offset) => Text.AsSpan(0, offset).Count('\n') + 1;
    internal string Body(SourceMethod method) => Text[method.Start..method.End];

    private int BlockEnd(int start)
    {
        int depth = 0;
        foreach (Match match in Regex.Matches(Text[start..], "\"(?:\\\\.|[^\"\\\\])*\"|//[^\\n]*|/\\*.*?\\*/|[{}]", RegexOptions.Singleline))
        {
            if (match.Value == "{") depth++;
            else if (match.Value == "}" && --depth == 0) return start + match.Index + match.Length;
        }
        throw new InvalidDataException("Unclosed source block: " + Name);
    }

    internal static List<string> Split(string text)
    {
        List<string> result = [];
        int depth = 0, begin = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if ("<([".Contains(text[i])) depth++;
            if (">)]".Contains(text[i])) depth--;
            if (text[i] == ',' && depth == 0)
            {
                result.Add(text[begin..i].Trim());
                begin = i + 1;
            }
        }
        if (text[begin..].Trim().Length != 0) result.Add(text[begin..].Trim());
        return result;
    }

    internal JsonObject Trace(SourceMethod method)
    {
        JsonArray reads = [], writes = [], flow = [];
        var lines = Body(method).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            JsonObject item = new() { ["line"] = Line(method.Start) + i, ["expression"] = lines[i].Trim() };
            if (Regex.IsMatch(lines[i], @"\.Read\w*\s*\(|\.Deserialize\s*\(|\.Decompress\s*\(")) reads.Add(item.DeepClone());
            if (Regex.IsMatch(lines[i], @"\.Write\w*\s*\(|\.Serialize\s*\(|\.Compress\s*\(")) writes.Add(item.DeepClone());
            if (Regex.IsMatch(lines[i], @"^\s*(if|else|for|foreach|while|switch|case)\b")) flow.Add(item.DeepClone());
        }
        return new() { ["source"] = Name, ["method"] = method.Name, ["signature"] = method.Signature,
            ["returnType"] = method.ReturnType, ["line"] = method.Line,
            ["reads"] = reads, ["writes"] = writes, ["controlFlow"] = flow };
    }

    internal IEnumerable<(SourceMethod Method, string[] Chain)> Related(SourceMethod method)
    {
        Queue<(SourceMethod, string[])> pending = new();
        HashSet<int> seen = [];
        pending.Enqueue((method, []));
        while (pending.TryDequeue(out var entry))
        {
            if (!seen.Add(entry.Item1.Start)) continue;
            yield return entry;
            foreach (var candidate in Methods)
                if (Regex.IsMatch(Body(entry.Item1), @"(?<![\w.])" + Regex.Escape(candidate.Name) + @"\s*\("))
                    pending.Enqueue((candidate, [.. entry.Item2, entry.Item1.Name]));
        }
    }
}
