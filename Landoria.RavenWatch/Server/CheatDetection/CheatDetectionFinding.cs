namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class CheatDetectionFinding
    {
        internal readonly Event[] source;
        internal readonly string detectionId;
        internal readonly string detectionCode;
        internal readonly string evidence;
        internal readonly string suspectedPlayerName;
        internal readonly string suspectedSessionId;
        internal readonly string anomaly;
        internal readonly string explanation;

        internal CheatDetectionFinding(Event[] source, string detectionId,
            string detectionCode, string evidence, string suspectedPlayerName,
            string suspectedSessionId, string anomaly, string explanation)
        {
            this.source = source;
            this.detectionId = detectionId;
            this.detectionCode = detectionCode;
            this.evidence = evidence;
            this.suspectedPlayerName = suspectedPlayerName;
            this.suspectedSessionId = suspectedSessionId;
            this.anomaly = anomaly;
            this.explanation = explanation;
        }
    }
}
