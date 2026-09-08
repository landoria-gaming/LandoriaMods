using System;
using Landoria.RavenWatch.Server.EventCollection;
using UnityEngine;

namespace Landoria.RavenWatch.Server
{
    internal static class ServerRuntime
    {
        internal static readonly Type[] PatchTypes =
        {
            typeof(EventRegistrationPatch), typeof(EventReadyPatch),
            typeof(ServerCreatureNetworkViewPatch), typeof(ServerCreatureStartedPatch),
            typeof(ServerHumanoidCreatureStartedPatch), typeof(ServerZdoPacketPatch),
            typeof(ServerNewZdoPatch)
        };

        private static float nextAnalysis;

        internal static void Open()
        {
            ReceivedJournal.Open();
            ServerEventPublisher.Open();
            nextAnalysis = Time.realtimeSinceStartup + 10f;
        }

        internal static void Tick()
        {
            if (RavenWatchPlugin.Log == null || Time.realtimeSinceStartup < nextAnalysis) return;
            nextAnalysis = Time.realtimeSinceStartup + 10f;
            EventAnalyzer.Analyze();
        }

        internal static void Close()
        {
            ServerEventPublisher.Close();
            ReceivedJournal.Close();
        }
    }
}
