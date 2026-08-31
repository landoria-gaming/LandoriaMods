using System.Collections.Generic;

namespace Landoria.Moderator
{
    internal static class WeatherCommands
    {
        internal static void Register()
        {
            new Terminal.ConsoleCommand("env", "[environment] - Override the weather.",
                ModeratorCommandAudit.Wrap(Set), isCheat: false, isNetwork: false,
                onlyServer: false, allowInDevBuild: true, optionsFetcher: GetEnvironments,
                onlyAdmin: true);
            new Terminal.ConsoleCommand("resetenv", "Restore automatic weather.",
                ModeratorCommandAudit.Wrap(Reset), isCheat: false, isNetwork: false,
                onlyServer: false, allowInDevBuild: true, onlyAdmin: true);
        }

        private static void Set(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 2)
            {
                args.Context.AddString("Usage: env <environment>");
                return;
            }
            string environment = string.Join(" ", args.Args, 1, args.Args.Length - 1);
            WeatherControlRpc.Request(environment);
            args.Context.AddString($"Weather change requested: {environment}");
        }

        private static void Reset(Terminal.ConsoleEventArgs args)
        {
            WeatherControlRpc.Request("");
            args.Context.AddString("Weather reset requested.");
        }

        private static List<string> GetEnvironments()
        {
            List<string> names = new List<string>();
            if (EnvMan.instance == null) return names;
            foreach (EnvSetup environment in EnvMan.instance.m_environments)
                names.Add(environment.m_name);
            return names;
        }
    }
}
