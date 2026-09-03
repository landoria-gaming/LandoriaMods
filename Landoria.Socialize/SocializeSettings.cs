using Landoria.SharedLib;

namespace Landoria.Socialize
{
    internal sealed class SocializeSettings
    {
        private const float FixedShoutDistance = 70f;
        private const float FixedSayDistance = 15f;
        private bool serverInitialized;

        internal SocializeSettings()
        {
            ResetState();
        }

        internal bool RestrictPublicPositions { get; private set; }
        internal bool RestrictPublicPings { get; private set; }
        internal float ShoutDistance { get; private set; }
        internal float SayDistance { get; private set; }

        internal void InitializeServer(ModLog logger)
        {
            if (serverInitialized || !ServerRole.IsDedicatedServer) return;
            serverInitialized = true;
            LogSettings(logger);
        }

        private void LogSettings(ModLog logger)
        {
            logger.LogInfo($"Effective map settings: restrictPublicPositions=" +
                $"{RestrictPublicPositions}, restrictPublicPings={RestrictPublicPings}.");
            logger.LogInfo($"Effective chat settings: shoutDistance={ShoutDistance}, " +
                $"sayDistance={SayDistance}.");
        }

        internal void WriteState(ZPackage package)
        {
            package.Write(RestrictPublicPositions);
            package.Write(RestrictPublicPings);
            package.Write(ShoutDistance);
            package.Write(SayDistance);
        }

        internal void ReadState(ZPackage package)
        {
            RestrictPublicPositions = package.ReadBool();
            RestrictPublicPings = package.ReadBool();
            ShoutDistance = package.ReadSingle();
            SayDistance = package.ReadSingle();
        }

        internal void ResetState()
        {
            if (serverInitialized) return;
            RestrictPublicPositions = true;
            RestrictPublicPings = true;
            ShoutDistance = FixedShoutDistance;
            SayDistance = FixedSayDistance;
        }
    }
}
