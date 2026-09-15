using BepInEx.Configuration;

namespace Landoria.HuginnCam
{
    internal static class HuginnCamPreference
    {
        private static ConfigEntry<string> _ffmpegPath;

        internal static string FfmpegPath => _ffmpegPath.Value?.Trim();

        internal static void Initialize(ConfigFile config)
        {
            _ffmpegPath = config.Bind(
                "Recording",
                "FfmpegPath",
                string.Empty,
                "Full path to ffmpeg.exe. Leave empty to resolve ffmpeg.exe from PATH.");
        }
    }
}
