using System.Collections.Generic;
using Landoria.RavenWatch.Server;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal interface ICheatDetection
    {
        CheatDetectionFinding[] ServerBasedDetection(IReadOnlyList<Event> events);
        CheatDetectionFinding[] ObserverBasedDetection(IReadOnlyList<Event> events);
    }
}
