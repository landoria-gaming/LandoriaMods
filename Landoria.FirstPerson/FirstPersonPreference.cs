using BepInEx.Configuration;

namespace Landoria.FirstPerson
{
    // Reads and saves the player's first-person settings.
    internal static class FirstPersonPreference
    {
        internal const float DefaultFieldOfView = 65f; // Degrees.
        internal const float MaximumFieldOfView = 120f; // Degrees.

        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<float> fieldOfView;

        internal static bool Enabled => enabled.Value;
        internal static float FieldOfView => fieldOfView.Value;

        // Creates the saved configuration entries used by the mod.
        internal static void Initialize(ConfigFile config)
        {
            enabled = config.Bind(
                "Camera", "FirstPersonEnabled", false,
                "Whether first-person view is enabled at minimum camera zoom.");
            fieldOfView = config.Bind(
                "Camera", "FieldOfView", DefaultFieldOfView,
                "Field of view shared by first-person, third-person, and free-fly cameras.");
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
            fieldOfView.Value = System.Math.Min(value, MaximumFieldOfView);
        }
    }
}
