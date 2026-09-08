using System.Collections.Generic;
using Landoria.RavenWatch.Server;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal interface ICheatDetection
    {
        string GetDetectionId(Event eventToAnalyze);
        void Detect(IReadOnlyList<Event> events);
    }
}
