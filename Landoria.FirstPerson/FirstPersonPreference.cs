using BepInEx.Configuration;

namespace Landoria.FirstPerson
{
    // Reads and saves the player's first-person settings.
    internal static class FirstPersonPreference
    {
        internal const float DefaultFieldOfView = 65f; // Degrees.
        internal const float MinimumFieldOfView = 65f; // Degrees.
        internal const float MaximumFieldOfView = 120f; // Degrees.
        internal const float DefaultCombatReturnDelay = 1f; // Seconds.
        internal const float DefaultZoomReturnDelay = 3f; // Seconds.
        internal const int DefaultHeadBobStrength = 2;

        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<float> fieldOfView;
        private static ConfigEntry<KeyboardShortcut> toggleShortcut;
        private static ConfigEntry<float> combatReturnDelay;
        private static ConfigEntry<float> zoomReturnDelay;
        private static ConfigEntry<int> headBobStrength;

        internal static bool Enabled => enabled.Value;
        internal static float FieldOfView => fieldOfView.Value;
        internal static KeyboardShortcut ToggleShortcut => toggleShortcut.Value;
        internal static float CombatReturnDelay => combatReturnDelay.Value;
        internal static float ZoomReturnDelay => zoomReturnDelay.Value;
        internal static int HeadBobStrength => headBobStrength.Value;

        // Creates the saved configuration entries used by the mod.
        internal static void Initialize(ConfigFile config)
        {
            enabled = config.Bind(
                "Camera", "FirstPersonEnabled", false,
                "Whether first-person view is enabled at minimum camera zoom.");
            fieldOfView = config.Bind(
                "Camera", "FieldOfView", DefaultFieldOfView,
                new ConfigDescription(
                    "Field of view shared by first-person, third-person, and free-fly cameras.",
                    new AcceptableValueRange<float>(
                        MinimumFieldOfView, MaximumFieldOfView)));
            toggleShortcut = config.Bind(
                "Controls", "ToggleShortcut",
                new KeyboardShortcut(UnityEngine.KeyCode.F6),
                "Shortcut used to enable or disable automatic first-person view.\n" +
                "\nExamples: Mouse2 for the middle mouse button.\n" +
                "\nMouse3/Mouse4 for the Forward/Back side button.\n" +
                "\nSpace + LeftControl for Left Ctrl + Space.\n" +
                "\nhttps://docs.unity3d.com/ScriptReference/KeyCode.html");
            combatReturnDelay = config.Bind(
                "Transitions", "CombatReturnDelay", DefaultCombatReturnDelay,
                "Seconds to remain in third person after an attack or block ends. " +
                "Set to 0 to disable temporary third person for combat.");
            zoomReturnDelay = config.Bind(
                "Transitions", "ZoomReturnDelay", DefaultZoomReturnDelay,
                "Seconds to remain in third person after the last camera zoom. " +
                "Set to 0 to disable temporary third person for zoom.");
            headBobStrength = config.Bind(
                "Camera", "HeadBobStrength", DefaultHeadBobStrength,
                new ConfigDescription(
                    "First-person head bob strength. Set to 0 to disable head bob.",
                    new AcceptableValueRange<int>(0, 3)));
            SetFieldOfView(fieldOfView.Value);
        }

        // Saves whether first person is enabled.
        internal static void SetEnabled(bool value)
        {
            enabled.Value = value;
        }

        // Saves a field of view after applying its supported limit.
        internal static void SetFieldOfView(float value)
        {
            fieldOfView.Value = System.Math.Max(
                MinimumFieldOfView,
                System.Math.Min(value, MaximumFieldOfView));
        }
    }
}
