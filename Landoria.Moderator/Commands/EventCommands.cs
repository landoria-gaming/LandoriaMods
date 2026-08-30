using System.Collections.Generic;

namespace Landoria.Moderator
{
    internal static class EventCommands
    {
        internal static void Register()
        {
            new Terminal.ConsoleCommand("event", "[name]", ModeratorCommandAudit.Wrap(Start),
                isCheat: false, isNetwork: false, onlyServer: false,
                allowInDevBuild: true, optionsFetcher: GetEventNames, onlyAdmin: true);
            new Terminal.ConsoleCommand("stopevent", "", ModeratorCommandAudit.Wrap(Stop),
                isCheat: false, isNetwork: false, onlyServer: false,
                allowInDevBuild: true, onlyAdmin: true);
        }

        private static void Start(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 2)
            {
                args.Context.AddString("Usage: event <name>");
                return;
            }
            EventControlRpc.RequestStart(args[1]);
            args.Context.AddString($"Event start requested: {args[1]}");
        }

        private static void Stop(Terminal.ConsoleEventArgs args)
        {
            EventControlRpc.RequestStop();
            args.Context.AddString("Event stop requested.");
        }

        private static List<string> GetEventNames()
        {
            List<string> names = new List<string>();
            if (RandEventSystem.instance == null) return names;
            foreach (RandomEvent randomEvent in RandEventSystem.instance.m_events)
            {
                if (randomEvent.m_enabled) names.Add(randomEvent.m_name);
            }
            return names;
        }
    }
}
