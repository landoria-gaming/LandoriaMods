namespace Landoria.FirstPerson
{
    // Registers and handles the command that toggles first person.
    internal static class FirstPersonCommand
    {
        // Makes the firstperson command available in the Valheim console.
        internal static void Register()
        {
            new Terminal.ConsoleCommand("firstperson", string.Empty, Run);
        }

        private static object Run(Terminal.ConsoleEventArgs args)
        {
            bool enabled = !FirstPersonMode.Enabled;
            FirstPersonMode.SetEnabled(enabled);
            FirstPersonPreference.SetEnabled(enabled);
            return true;
        }
    }
}
