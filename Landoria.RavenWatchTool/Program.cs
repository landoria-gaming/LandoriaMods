using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Landoria.RavenWatchTool;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args.Contains("--help"))
            {
                Console.WriteLine("Landoria.RavenWatchTool --source <dedicated server source directory> [--output <output JSON>]");
                return args.Length == 0 ? 1 : 0;
            }
            Run(args);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run(string[] args)
    {
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || !new[] { "--source", "--output" }.Contains(args[i]))
                throw new ArgumentException("Invalid command line option: " + args[i]);
            options.Add(args[i], args[i + 1]);
        }
        if (!options.TryGetValue("--source", out var source)) throw new ArgumentException("--source is required.");
        var reviewed = WireLayouts.Generate(Path.GetFullPath(source));
        string output = Path.GetFullPath(options.GetValueOrDefault("--output", Path.Combine("Landoria.RavenWatch", "Resources", $"valheim-{reviewed["gameVersion"]}-rpc.json")));
        var document = DefinitionBuilder.Generate(Path.GetFullPath(source), reviewed);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        string temporary = output + ".tmp";
        File.WriteAllText(temporary, document.ToJsonString(new JsonSerializerOptions {
            WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) }) + "\n", new UTF8Encoding(false));
        File.Move(temporary, output, true);
        Console.WriteLine($"Generated {document["rpcCount"]} registrations / {document["uniqueNames"]} names: {output}");
    }
}
