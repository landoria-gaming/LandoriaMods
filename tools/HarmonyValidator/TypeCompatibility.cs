using Mono.Cecil;

namespace Landoria.Build;

// Checks metadata assignability and leaves complex CLR conversions unverified.
internal static class TypeCompatibility
{
    internal static TypeReference Value(TypeReference type) =>
        type is ByReferenceType reference ? reference.ElementType : type;

    // Null means that this conversion needs runtime or generic type information.
    internal static bool? Assignable(TypeReference source, TypeReference destination)
    {
        if (source.FullName == destination.FullName)
        {
            return true;
        }

        if (source.ContainsGenericParameter || destination.ContainsGenericParameter
            || source is PointerType || destination is PointerType
            || source is ByReferenceType || destination is ByReferenceType)
        {
            return null;
        }

        if (destination.FullName == "System.Object")
        {
            return source.FullName != "System.Void";
        }

        if (destination.IsValueType && destination is not GenericInstanceType)
        {
            return false;
        }

        if (source is ArrayType || destination is ArrayType
            || source is GenericInstanceType || destination is GenericInstanceType)
        {
            return null;
        }

        return Ancestor(source.Resolve(), destination, new HashSet<string>());
    }

    private static bool? Ancestor(TypeDefinition source, TypeReference destination,
        HashSet<string> visited)
    {
        if (!visited.Add(source.FullName))
        {
            return false;
        }

        if (source.FullName == destination.FullName)
        {
            return true;
        }

        bool uncertain = false;
        var parents = source.Interfaces.Select(i => i.InterfaceType).ToList();
        if (source.BaseType != null)
        {
            parents.Add(source.BaseType);
        }

        foreach (var parent in parents)
        {
            if (parent is GenericInstanceType)
            {
                uncertain = true;
                continue;
            }

            var match = Ancestor(parent.Resolve(), destination, visited);
            if (match == true)
            {
                return true;
            }

            uncertain |= match == null;
        }

        return uncertain ? null : false;
    }
}
