using System;

namespace Landoria.RavenWatch
{
    public static class RavenWatchApi
    {
        public static event Action<CheatDetectionReport> CheatReported;

        internal static void Publish(CheatDetectionReport report)
        {
            Action<CheatDetectionReport> handlers = CheatReported;
            if (handlers == null) return;
            foreach (Action<CheatDetectionReport> handler in handlers.GetInvocationList())
            {
                try { handler(report); }
                catch (Exception exception)
                {
                    RavenWatchPlugin.Log.LogError(
                        "A RavenWatch cheat report subscriber failed: " + exception);
                }
            }
        }
    }
}
