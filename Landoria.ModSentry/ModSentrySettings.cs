using System;

namespace Landoria.ModSentry
{
    internal static class ModSentrySettings
    {
        internal const string KnownCheatProtectionArgument =
            "--modsentry-known-cheat-protection";

        internal const string KnownCheatActionArgument =
            "--modsentry-known-cheat-action";

        internal static string KnownCheatAction { get; private set; } = "kick";

        internal static bool KnownCheatProtectionEnabled { get; private set; }

        internal static void Initialize()
        {
            KnownCheatAction = ResolveAction(Environment.GetCommandLineArgs());
            KnownCheatProtectionEnabled = ResolveBoolean(
                Environment.GetCommandLineArgs(),
                KnownCheatProtectionArgument, true);
        }

        private static string ResolveAction(string[] arguments)
        {
            string value = ResolveValue(arguments, KnownCheatActionArgument);
            if (value == null)
            {
                return "kick";
            }
            if (string.Equals(value, "kick", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "ban", StringComparison.OrdinalIgnoreCase))
            {
                return value.ToLowerInvariant();
            }
            throw new InvalidOperationException(
                $"Command-line switch {KnownCheatActionArgument} requires kick or ban.");
        }

        private static bool ResolveBoolean(string[] arguments, string name,
            bool defaultValue)
        {
            string value = ResolveValue(arguments, name);
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

        private static string ResolveValue(string[] arguments, string name)
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
            return value;
        }
    }
}
