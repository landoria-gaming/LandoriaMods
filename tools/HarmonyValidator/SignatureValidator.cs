using Mono.Cecil;

namespace Landoria.Build;

// Validates patch signatures without executing Harmony or the target assembly.
internal sealed class SignatureValidator(Action<string, string, bool> report)
{
    private MethodDefinition patch = null!;
    private MethodDefinition original = null!;
    private string kind = "";

    // Check return types, receiver requirements and each injected argument.
    internal void Check(MethodDefinition method, MethodDefinition target)
    {
        patch = method;
        original = target;
        kind = Kind(method);
        if (!patch.IsStatic || patch.HasGenericParameters)
        {
            Warn("Instance or generic patch signature was not verified.");
            return;
        }

        if (kind == "ReversePatch")
        {
            Reverse();
            return;
        }

        if (kind == "Transpiler" || kind.Length == 0)
        {
            Warn("Transpiler or unknown patch signature was not verified.");
            return;
        }

        int skip = ReturnValue();
        foreach (var parameter in patch.Parameters.Skip(skip))
        {
            Parameter(parameter);
        }
    }

    internal static string Kind(MethodDefinition method)
    {
        string[] kinds = ["ReversePatch", "Prefix", "Postfix", "Finalizer",
            "Transpiler"];
        var selected = kinds.Where(k => Has(method, "Harmony" + k)).ToArray();
        return selected.Length == 1 ? selected[0]
            : selected.Length > 1 ? "" : method.Name;
    }

    internal static bool Has(ICustomAttributeProvider provider, string attribute) =>
        provider.CustomAttributes.Any(a =>
            a.AttributeType.FullName == "HarmonyLib." + attribute);

    private int ReturnValue()
    {
        string returned = patch.ReturnType.FullName;
        if (returned == "System.Void")
        {
            return 0;
        }

        if (kind == "Prefix")
        {
            Require(returned == "System.Boolean", "Prefix must return void or bool.");
        }
        else if (kind == "Finalizer")
        {
            Compatible(patch.ReturnType, patch.Module.ImportReference(typeof(Exception)),
                "Finalizer return");
        }
        else if (kind == "Postfix")
        {
            Require(patch.Parameters.Count > 0 && patch.Parameters[0].ParameterType
                .FullName == returned, "Pass-through postfix must take its return "
                + "type as the first parameter.");
            Compatible(original.ReturnType, patch.ReturnType, "Postfix input");
            Compatible(patch.ReturnType, original.ReturnType, "Postfix return");
            return 1;
        }

        return 0;
    }

    private void Reverse()
    {
        var expected = original.Parameters.Select(p => p.ParameterType).ToList();
        if (!original.IsStatic)
        {
            TypeReference receiver = original.DeclaringType;
            expected.Insert(0, receiver.IsValueType
                ? new ByReferenceType(receiver) : receiver);
        }

        Require(expected.Count == patch.Parameters.Count,
            "Reverse patch must reproduce the target arguments and instance.");
        for (int i = 0; i < Math.Min(expected.Count, patch.Parameters.Count); i++)
        {
            ExactOrUnverified(expected[i], patch.Parameters[i].ParameterType,
                "Reverse argument " + i);
        }

        ExactOrUnverified(original.ReturnType, patch.ReturnType, "Reverse return");
    }

    private void Parameter(ParameterDefinition parameter)
    {
        if (Has(parameter, "HarmonyArgument") || Has(patch, "HarmonyArgument")
            || Has(patch.DeclaringType, "HarmonyArgument"))
        {
            Warn(parameter.Name + ": HarmonyArgument mapping was not verified.");
            return;
        }

        if (Special(parameter))
        {
            return;
        }

        var argument = original.Parameters.FirstOrDefault(p => p.Name == parameter.Name);
        if (parameter.Name.StartsWith("__", StringComparison.Ordinal))
        {
            argument = int.TryParse(parameter.Name[2..], out int index)
                && index >= 0 && index < original.Parameters.Count
                ? original.Parameters[index] : null;
        }

        if (argument == null)
        {
            Missing(parameter);
            return;
        }

        Inject(argument.ParameterType, parameter, true);
    }

    private void Missing(ParameterDefinition parameter)
    {
        var type = TypeCompatibility.Value(parameter.ParameterType).Resolve();
        if (type.BaseType?.FullName == "System.MulticastDelegate")
        {
            Warn(parameter.Name + ": Delegate injection was not verified.");
            return;
        }

        Error("Unknown argument or injection: " + parameter.Name);
    }

    private bool Special(ParameterDefinition parameter)
    {
        switch (parameter.Name)
        {
            case "__instance":
                Instance(parameter);
                return true;
            case "__result":
                Result(parameter);
                return true;
            case "__state":
                State(parameter);
                return true;
            case "__args":
                Require(parameter.ParameterType.FullName == "System.Object[]",
                    "__args must have type object[].");
                return true;
            case "__runOriginal":
                Inject(patch.Module.TypeSystem.Boolean, parameter, false);
                return true;
            case "__exception":
                Require(kind == "Finalizer", "__exception requires a finalizer.");
                Inject(patch.Module.ImportReference(typeof(Exception)), parameter, false);
                return true;
            case "__originalMethod":
                OriginalMethod(parameter);
                return true;
            case "__resultRef":
                Warn("__resultRef support depends on the Harmony runtime version.");
                return true;
        }

        if (!parameter.Name.StartsWith("___", StringComparison.Ordinal))
        {
            return false;
        }

        Field(parameter);
        return true;
    }

    private void Instance(ParameterDefinition parameter)
    {
        if (original.IsStatic)
        {
            Require(!parameter.ParameterType.IsByReference
                && !parameter.ParameterType.IsValueType,
                "A static target supplies null for __instance.");
            return;
        }

        Inject(original.DeclaringType, parameter, false);
    }

    private void Result(ParameterDefinition parameter)
    {
        if (original.ReturnType.FullName == "System.Void")
        {
            Error("A void target cannot supply __result.");
            return;
        }

        if (original.ReturnType.IsByReference)
        {
            Warn("Ref-return result injection was not verified.");
            return;
        }

        Inject(original.ReturnType, parameter, true);
    }

    private void OriginalMethod(ParameterDefinition parameter)
    {
        Require(!parameter.ParameterType.IsByReference,
            "__originalMethod cannot be passed by reference.");
        Type type = original.IsConstructor ? typeof(System.Reflection.ConstructorInfo)
            : typeof(System.Reflection.MethodInfo);
        Compatible(patch.Module.ImportReference(type), parameter.ParameterType,
            "__originalMethod");
    }

    private void Field(ParameterDefinition parameter)
    {
        string name = parameter.Name[3..];
        if (int.TryParse(name, out _))
        {
            Warn(parameter.Name + ": Runtime field ordering was not verified.");
            return;
        }

        var owner = original.DeclaringType;
        FieldDefinition? field = null;
        while (owner != null && field == null)
        {
            field = owner.Fields.FirstOrDefault(f => f.Name == name);
            owner = owner.BaseType?.Resolve();
        }

        if (field == null)
        {
            Error("Injected field not found: " + name);
            return;
        }

        Require(field.IsStatic || !original.IsStatic,
            "A static target cannot supply instance field " + name);
        Inject(field.FieldType, parameter, false);
    }

    private void State(ParameterDefinition parameter)
    {
        var producers = patch.DeclaringType.Methods.Where(m => Kind(m) == "Prefix")
            .SelectMany(m => m.Parameters).Where(p => p.Name == "__state").ToArray();
        if (producers.Length != 1)
        {
            Warn("__state producer or target pairing was not verified.");
            return;
        }

        var producer = producers[0];
        var target = new PatchTarget();
        target.Apply(patch.DeclaringType.CustomAttributes);
        target.Apply(((MethodDefinition)producer.Method).CustomAttributes);
        if (target.Type == null || target.Unsupported || !Validator.Candidates(target)
            .Any(m => m.FullName == original.FullName))
        {
            Warn("__state producer targets a different or unresolved method.");
            return;
        }

        if (!producer.ParameterType.IsByReference)
        {
            Warn("__state prefix does not write through ref/out.");
        }

        Inject(producer.ParameterType, parameter, false);
    }

    private void Inject(TypeReference source, ParameterDefinition parameter,
        bool allowsBoxing)
    {
        source = TypeCompatibility.Value(source);
        var destination = TypeCompatibility.Value(parameter.ParameterType);
        if (parameter.ParameterType.IsByReference
            && source.FullName != destination.FullName)
        {
            if (allowsBoxing && destination.FullName == "System.Object"
                && source.IsValueType)
            {
                return;
            }

            ExactOrUnverified(source, destination, parameter.Name + " ref/out");
            return;
        }

        if (!allowsBoxing && source.IsValueType && !destination.IsValueType)
        {
            Error(parameter.Name + ": This injection does not box value types.");
            return;
        }

        Compatible(source, destination, parameter.Name);
    }

    private void ExactOrUnverified(TypeReference source, TypeReference destination,
        string label)
    {
        if (source.FullName == destination.FullName)
        {
            return;
        }

        var compatible = TypeCompatibility.Assignable(source, destination);
        if (compatible == false)
        {
            Error(label + ": Incompatible types " + source.FullName + " and "
                + destination.FullName);
        }
        else
        {
            Warn(label + ": Non-identical storage types were not verified.");
        }
    }

    private void Compatible(TypeReference source, TypeReference destination, string label)
    {
        var compatible = TypeCompatibility.Assignable(source, destination);
        if (compatible == null)
        {
            Warn(label + ": Type conversion was not verified.");
        }
        else if (compatible == false)
        {
            Error(label + ": Cannot assign " + source.FullName + " to "
                + destination.FullName);
        }
    }

    private void Require(bool condition, string message)
    {
        if (!condition)
        {
            Error(message);
        }
    }

    private void Error(string message) => report("SHV004", message, false);
    private void Warn(string message) => report("SHV103", message, true);
}
