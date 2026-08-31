namespace Landoria.Moderator
{
    internal static class NextDayCommand
    {
        internal static void Register()
        {
            new Terminal.ConsoleCommand("nextday", "Advance to the next morning.",
                ModeratorCommandAudit.Wrap(Execute), isCheat: false,
                isNetwork: false, onlyServer: false, allowInDevBuild: true,
                onlyAdmin: true);
        }

        private static void Execute(Terminal.ConsoleEventArgs args)
        {
            NextDayControlRpc.Request();
            args.Context.AddString("Skip to the next morning requested.");
        }
    }
}
