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

        private static readonly ProcessSignature[] ProcessSignatures =
        {
            new ProcessSignature("Valheim Mod Menu", "valheimmodmenuloader", MatchKind.Contains),
            new ProcessSignature("SharpMonoInjector", "smi", MatchKind.Exact),
            new ProcessSignature("SharpMonoInjector", "smi_gui", MatchKind.Exact),
            new ProcessSignature("Xenos Injector", "xenos", MatchKind.Exact),
            new ProcessSignature("Xenos Injector", "xenos64", MatchKind.Exact),
            new ProcessSignature("Extreme Injector", "extreme injector", MatchKind.Prefix)
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

        internal static bool TryMatchProcess(string value, out string tool)
        {
            foreach (ProcessSignature signature in ProcessSignatures)
            {
                if (signature.Matches(value))
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
                : vector == "type_namespace"
                    ? TryMatchNamespace(indicator, out matched) &&
                        string.Equals(tool, matched, StringComparison.Ordinal)
                    : vector == "process_name" &&
                        TryMatchProcess(indicator, out matched) &&
                        string.Equals(tool, matched, StringComparison.Ordinal);
        }

        private enum MatchKind
        {
            Exact,
            Prefix,
            Contains
        }

        private sealed class ProcessSignature
        {
            private readonly string _marker;
            private readonly MatchKind _kind;

            internal ProcessSignature(string tool, string marker, MatchKind kind)
            {
                Tool = tool;
                _marker = marker;
                _kind = kind;
            }

            internal string Tool { get; }

            internal bool Matches(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return false;
                int index = value.IndexOf(_marker, StringComparison.OrdinalIgnoreCase);
                return _kind == MatchKind.Contains ? index >= 0 :
                    _kind == MatchKind.Prefix ? index == 0 :
                    string.Equals(value, _marker, StringComparison.OrdinalIgnoreCase);
            }
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
