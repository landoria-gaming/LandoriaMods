using System;

namespace Landoria.StructureProtection
{
    internal static class StructureProtectionArgumentPolicy
    {
        internal static bool Resolve(string[] arguments, string name, bool defaultValue)
        {
            string value = ReadValue(arguments, name);
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

        internal static int ResolveMinimum(
            string[] arguments, string name, int defaultValue, int minimum)
        {
            string value = ReadValue(arguments, name);
            if (value == null)
            {
                return defaultValue;
            }
            if (int.TryParse(value, out int result) && result >= minimum)
            {
                return result;
            }
            throw new InvalidOperationException(
                $"Command-line switch {name} requires an integer of at least {minimum}.");
        }

        private static string ReadValue(string[] arguments, string name)
        {
            string value = null;
            for (int index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
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
