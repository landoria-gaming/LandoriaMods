using System.Collections.Generic;
using Landoria.RavenWatch.Server;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal interface ICheatDetection
    {
        string GetDetectionId(Event eventToAnalyze);
        void ServerBasedDetection(IReadOnlyList<Event> events);
        void ObserverBasedDetection(IReadOnlyList<Event> events);
    }
}
