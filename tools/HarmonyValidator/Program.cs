using Mono.Cecil;

namespace Landoria.Build;

// Runs the build validator without loading Unity or installing Harmony patches.
internal static class Program
{
    internal static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"HarmonyValidator : error SHV000: {error}");
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length != 2)
        {
            throw new ArgumentException("Expected assembly path and reference list.");
        }

        using var resolver = new DefaultAssemblyResolver();
        foreach (var directory in File.ReadAllLines(args[1])
            .Append(args[0]).Select(Path.GetDirectoryName).Distinct())
        {
            if (!string.IsNullOrEmpty(directory))
            {
                resolver.AddSearchDirectory(directory);
            }
        }

        using var assembly = AssemblyDefinition.ReadAssembly(args[0],
            new ReaderParameters { AssemblyResolver = resolver });
        var validator = new Validator(args[0]);
        validator.Check(assembly.MainModule.Types);
        Console.WriteLine($"Harmony targets: {validator.Checked} checked, "
            + $"{validator.Errors} errors, {validator.Warnings} unverified.");
        return validator.Errors == 0 ? 0 : 1;
    }
}
