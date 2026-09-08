using System;
using System.Collections.Generic;
using System.Threading;
using Landoria.RavenWatch.Server.CheatDetection;

namespace Landoria.RavenWatch.Server
{
    internal static class EventAnalyzer
    {
        private static readonly ICheatDetection[] detections =
        {
            new CreatureSpawnedDetection()
        };
        private static int analyzing;

        internal static void Analyze()
        {
            if (Interlocked.CompareExchange(ref analyzing, 1, 0) != 0) return;
            try { AnalyzeAndClean(); }
            finally { Volatile.Write(ref analyzing, 0); }
        }

        private static void AnalyzeAndClean()
        {
            IReadOnlyList<Event> events = ReceivedJournal.GetBufferedEvents();
            AnalysisJournal.Overwrite(events);
            bool succeeded = true;
            foreach (ICheatDetection detection in detections)
            {
                if (!RunServerBasedDetection(detection, events)) succeeded = false;
                if (!RunObserverBasedDetection(detection, events)) succeeded = false;
            }
            if (succeeded) ReceivedJournal.ArchiveOverflow();
        }

        private static bool RunServerBasedDetection(
            ICheatDetection detection, IReadOnlyList<Event> events)
        {
            try
            {
                detection.ServerBasedDetection(events);
                return true;
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError(exception);
                return false;
            }
        }

        private static bool RunObserverBasedDetection(
            ICheatDetection detection, IReadOnlyList<Event> events)
        {
            try
            {
                detection.ObserverBasedDetection(events);
                return true;
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError(exception);
                return false;
            }
        }
    }
}
