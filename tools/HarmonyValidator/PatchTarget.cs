using Mono.Cecil;

namespace Landoria.Build;

// Combines target annotations declared on a patch class and its methods.
internal sealed class PatchTarget
{
    internal TypeReference? Type;
    internal string? Name;
    internal int Kind;
    internal TypeReference[]? Arguments;
    internal int[]? Variations;
    internal bool Unsupported;

    internal PatchTarget Copy() => (PatchTarget)MemberwiseClone();

    // Read only metadata; never load or execute the mod or game assemblies.
    internal void Apply(IEnumerable<CustomAttribute> attributes)
    {
        foreach (var attribute in attributes.Where(a =>
            a.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
        {
            if (attribute.ConstructorArguments.Count > 1
                && attribute.ConstructorArguments[0].Type.FullName == "System.String"
                && attribute.ConstructorArguments[1].Type.FullName == "System.String")
            {
                Unsupported = true;
            }

            foreach (var argument in attribute.ConstructorArguments)
            {
                Read(argument);
            }
        }
    }

    private void Read(CustomAttributeArgument argument)
    {
        switch (argument.Type.FullName)
        {
            case "System.Type":
                Type = (TypeReference)argument.Value;
                break;
            case "System.String":
                Name = (string)argument.Value;
                break;
            case "HarmonyLib.MethodType":
                Kind = Convert.ToInt32(argument.Value);
                break;
            case "System.Type[]":
                Arguments = Values(argument).Select(a =>
                    (TypeReference)a.Value).ToArray();
                break;
            case "HarmonyLib.ArgumentType[]":
                Variations = Values(argument).Select(a =>
                    Convert.ToInt32(a.Value)).ToArray();
                break;
            default:
                Unsupported = true;
                break;
        }
    }

    private static CustomAttributeArgument[] Values(CustomAttributeArgument argument) =>
        argument.Value as CustomAttributeArgument[] ?? [];

    internal string MethodName => Kind switch
    {
        0 => Name ?? "",
        1 => "get_" + Name,
        2 => "set_" + Name,
        3 => ".ctor",
        4 => ".cctor",
        _ => ""
    };

    internal string ArgumentName(int index)
    {
        string name = Arguments![index].FullName;
        int variation = Variations != null && index < Variations.Length
            ? Variations[index] : 0;
        return variation switch
        {
            1 or 2 => name + "&",
            3 => name + "*",
            _ => name
        };
    }
}
