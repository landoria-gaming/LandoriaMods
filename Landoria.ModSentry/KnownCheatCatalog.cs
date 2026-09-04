using System;

namespace Landoria.ModSentry
{
    internal static class KnownCheatCatalog
    {
        private static readonly Signature[] Signatures =
        {
            new Signature("ValheimTooler", "ValheimTooler", true, true),
            new Signature("ValheimHack223", "ValheimHack223", false, true),
            new Signature("valheim-hax", "valheim-hax", false, false)
        };

        internal static bool TryMatchAssembly(string value, out string tool)
        {
            foreach (Signature signature in Signatures)
            {
                if (signature.MatchesAssembly(value))
                {
                    tool = signature.Tool;
                    return true;
                }
            }
            tool = null;
            return false;
        }

        internal static bool TryMatchNamespace(string value, out string tool)
        {
            foreach (Signature signature in Signatures)
            {
                if (signature.MatchesNamespace(value))
                {
                    tool = signature.Tool;
                    return true;
                }
            }
            tool = null;
            return false;
        }

        internal static bool Matches(string tool, string vector,
            string indicator)
        {
            return vector == "assembly_name"
                ? TryMatchAssembly(indicator, out string matched) &&
                    string.Equals(tool, matched, StringComparison.Ordinal)
                : vector == "type_namespace" &&
                    TryMatchNamespace(indicator, out matched) &&
                    string.Equals(tool, matched, StringComparison.Ordinal);
        }

        private sealed class Signature
        {
            private readonly string _marker;
            private readonly bool _assemblyContains;
            private readonly bool _namespacePrefix;

            internal Signature(string tool, string marker,
                bool assemblyContains, bool namespacePrefix)
            {
                Tool = tool;
                _marker = marker;
                _assemblyContains = assemblyContains;
                _namespacePrefix = namespacePrefix;
            }

            internal string Tool { get; }

            internal bool MatchesAssembly(string value)
            {
                return _assemblyContains
                    ? value?.IndexOf(_marker,
                        StringComparison.OrdinalIgnoreCase) >= 0
                    : string.Equals(value, _marker,
                        StringComparison.OrdinalIgnoreCase);
            }

            internal bool MatchesNamespace(string value)
            {
                return _namespacePrefix &&
                    (string.Equals(value, _marker,
                        StringComparison.OrdinalIgnoreCase) ||
                    value?.StartsWith(_marker + ".",
                        StringComparison.OrdinalIgnoreCase) == true);
            }
        }
    }
}
