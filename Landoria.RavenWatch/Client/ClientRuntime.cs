using UnityEngine;

namespace Landoria.RavenWatch.Client
{
    internal static class ClientRuntime
    {
        private static float nextFlush;

        internal static void Open()
        {
            ActivityJournal.Open();
            PlayerDebugFlyObserver.Open();
            nextFlush = Time.realtimeSinceStartup + 5f;
        }

        internal static void Tick()
        {
            PlayerDebugFlyObserver.Tick();
            EventTransport.Send();
            if (RavenWatchPlugin.Log == null || Time.realtimeSinceStartup < nextFlush) return;
            nextFlush = Time.realtimeSinceStartup + 5f;
            ActivityJournal.Flush();
            EventTransport.Send();
        }

        internal static void Close()
        {
            ActivityJournal.Close();
            EventTransport.Send();
        }
    }
}
