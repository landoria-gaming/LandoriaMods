namespace Landoria.RavenWatch.Client
{
    internal static class ToggleRavenWatchCommand
    {
        internal static void Register()
            => new Terminal.ConsoleCommand("toggleRavenWatch",
                "Toggle client inventory events. Full inventory replies stay enabled.", Run);

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            InventoryEventSender.EventsEnabled = !InventoryEventSender.EventsEnabled;
            args.Context.AddString("RavenWatch client events: "
                + (InventoryEventSender.EventsEnabled ? "enabled" : "disabled")
                + ". Full inventory replies remain enabled.");
        }
    }
}
