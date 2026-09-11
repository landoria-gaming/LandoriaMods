using Mono.Cecil;

namespace Landoria.Build;

// Resolves declarative Harmony targets against the assemblies used by the build.
internal sealed class Validator(string assemblyPath)
{
    internal int Checked;
    internal int Errors;
    internal int Warnings;

    internal void Check(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            CheckType(type);
            Check(type.NestedTypes);
        }
    }

    private void CheckType(TypeDefinition type)
    {
        var target = new PatchTarget();
        target.Apply(type.CustomAttributes);
        bool marked = Has(type, "HarmonyPatch");
        bool dynamic = type.Methods.Any(m =>
            m.Name is "TargetMethod" or "TargetMethods"
            || Has(m, "HarmonyTargetMethod") || Has(m, "HarmonyTargetMethods"));
        foreach (var method in type.Methods.Where(m => IsPatch(m, marked)))
        {
            var selected = target.Copy();
            selected.Apply(method.CustomAttributes);
            if (dynamic && !Has(method, "HarmonyPatch"))
            {
                Report(method, "SHV101", "Dynamic target was not verified.", true);
                continue;
            }

            Resolve(method, selected);
        }
    }

    private static bool IsPatch(MethodDefinition method, bool marked) =>
        (marked && method.Name is "Prefix" or "Postfix" or "Finalizer" or "Transpiler")
        || Has(method, "HarmonyPrefix") || Has(method, "HarmonyPostfix")
        || Has(method, "HarmonyFinalizer") || Has(method, "HarmonyTranspiler")
        || Has(method, "HarmonyReversePatch");

    private static bool Has(ICustomAttributeProvider provider, string name) =>
        provider.CustomAttributes.Any(a => a.AttributeType.FullName == "HarmonyLib."
            + name);

    private void Resolve(MethodDefinition patch, PatchTarget target)
    {
        if (target.Unsupported || target.Kind is < 0 or > 4
            || target.Type is GenericInstanceType
            || target.Variations?.Any(value => value is < 0 or > 3) == true)
        {
            Report(patch, "SHV102", "Unsupported target annotation; not verified.", true);
            return;
        }

        if (target.Type == null || target.MethodName.Length == 0)
        {
            Report(patch, "SHV001", "Target type or method name is missing.");
            return;
        }

        try
        {
            ResolveMethods(patch, target);
        }
        catch (Exception error)
        {
            Report(patch, "SHV000", "Could not resolve target metadata. " + error);
        }
    }

    private void ResolveMethods(MethodDefinition patch, PatchTarget target)
    {
        Checked++;
        var methods = Candidates(target);
        string description = target.Type!.FullName + "." + target.MethodName;
        if (methods.Length == 0)
        {
            Report(patch, "SHV002", "Target not found: " + description);
        }
        else if (methods.Length > 1)
        {
            Report(patch, "SHV003", "Ambiguous target: " + description
                + ". Specify argument types. Candidates: "
                + string.Join("; ", methods.Select(m => m.FullName)));
        }
    }

    private static MethodDefinition[] Candidates(PatchTarget target)
    {
        var type = target.Type!.Resolve();
        while (type != null)
        {
            var matches = type.Methods.Where(m => m.Name == target.MethodName
                && Matches(m, target)).ToArray();
            if (matches.Length > 0 || target.Kind is 3 or 4)
            {
                return matches;
            }

            type = type.BaseType?.Resolve();
        }

        return [];
    }

    private static bool Matches(MethodDefinition method, PatchTarget target)
    {
        if (target.Arguments == null)
        {
            return true;
        }

        return method.Parameters.Count == target.Arguments.Length
            && method.Parameters.Select((p, i) =>
                p.ParameterType.FullName == target.ArgumentName(i)).All(equal => equal);
    }

    private void Report(MethodDefinition patch, string code, string message,
        bool warning = false)
    {
        if (warning)
        {
            Warnings++;
        }
        else
        {
            Errors++;
        }

        string level = warning ? "warning" : "error";
        Console.WriteLine($"{assemblyPath} : {level} {code}: "
            + $"{patch.DeclaringType.FullName}.{patch.Name}: {message}");
    }
}
