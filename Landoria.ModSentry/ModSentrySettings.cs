using System;

namespace Landoria.ModSentry
{
    internal static class ModSentrySettings
    {
        internal const string KnownCheatProtectionArgument =
            "--modsentry-known-cheat-protection";

        internal static bool KnownCheatProtectionEnabled { get; private set; }

        internal static void Initialize()
        {
            KnownCheatProtectionEnabled = ResolveBoolean(
                Environment.GetCommandLineArgs(),
                KnownCheatProtectionArgument, false);
        }

        private static bool ResolveBoolean(string[] arguments, string name,
            bool defaultValue)
        {
            string value = null;
            for (int index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (value != null || index + 1 >= arguments.Length)
                {
                    throw new InvalidOperationException(
                        $"Command-line switch {name} is missing or duplicated.");
                }
                value = arguments[++index];
            }
            if (value == null)
            {
                return defaultValue;
            }
            if (bool.TryParse(value, out bool enabled))
            {
                return enabled;
            }
            throw new InvalidOperationException(
                $"Command-line switch {name} requires true or false.");
        }
    }
}
