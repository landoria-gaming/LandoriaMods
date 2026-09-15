using System;
using System.Collections.Generic;
using System.Globalization;

namespace Landoria.FreeFly
{
    // Registers and handles free-fly console commands.
    internal static class FreeFlyCommands
    {
        // Registers the smoothing command.
        internal static void Register()
        {
            new Terminal.ConsoleCommand(
                "ffsmooth",
                "[0-1] sets native free-camera smoothing.",
                SetSmoothness,
                optionsFetcher: SmoothnessOptions);
        }

        // Applies a valid smoothing value.
        private static object SetSmoothness(Terminal.ConsoleEventArgs args)
        {
            if (args.Length != 2 ||
                !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture,
                    out float smoothness) ||
                smoothness < 0f || smoothness > 1f)
            {
                return "Use ffsmooth followed by a value from 0 to 1.";
            }

            GameCamera.instance.SetFreeFlySmoothness(smoothness);
            return true;
        }

        // Lists common smoothing values.
        private static List<string> SmoothnessOptions()
        {
            return new List<string> { "0", "0.25", "0.5", "0.75", "1" };
        }
    }
}
