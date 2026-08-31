namespace Landoria.DecayControl
{
    internal static class ShowDecayCommand
    {
        internal static void Register()
        {
            new Terminal.ConsoleCommand("showdecay", "Show or hide fuel and building health indicators.",
                Execute, isCheat: false, isNetwork: false, onlyServer: false,
                allowInDevBuild: true);
        }

        private static void Execute(Terminal.ConsoleEventArgs args)
        {
            DecayIndicators.Toggle();
            args.Context.AddString(
                $"Decay indicators: {(DecayIndicators.Enabled ? "on" : "off")}");
        }
    }
}
