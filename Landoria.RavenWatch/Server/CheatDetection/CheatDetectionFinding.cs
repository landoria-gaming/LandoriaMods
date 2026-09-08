using System;

namespace Landoria.RavenWatch.Server.CheatDetection
{
    internal sealed class CheatDetectionFinding
    {
        internal readonly Event[] source;
        internal readonly string detectionId;
        internal readonly string detectionCode;
        internal readonly int anomalyConfidence;
        internal readonly int attributionConfidence;
        internal readonly string evidence;
        internal readonly string suspectedPlayerName;
        internal readonly string suspectedSessionId;
        internal readonly string anomaly;
        internal readonly string explanation;

        internal CheatDetectionFinding(Event[] source, string detectionId,
            string detectionCode, int anomalyConfidence, int attributionConfidence,
            string evidence,
            string suspectedPlayerName, string suspectedSessionId,
            string anomaly, string explanation)
        {
            ValidateConfidence(anomalyConfidence, nameof(anomalyConfidence));
            ValidateConfidence(attributionConfidence, nameof(attributionConfidence));
            this.source = source;
            this.detectionId = detectionId;
            this.detectionCode = detectionCode;
            this.anomalyConfidence = anomalyConfidence;
            this.attributionConfidence = attributionConfidence;
            this.evidence = evidence;
            this.suspectedPlayerName = suspectedPlayerName;
            this.suspectedSessionId = suspectedSessionId;
            this.anomaly = anomaly;
            this.explanation = explanation;
        }

        private static void ValidateConfidence(int confidence, string parameterName)
        {
            if (confidence < 1 || confidence > 3)
                throw new ArgumentOutOfRangeException(parameterName,
                    "Confidence must be 1, 2 or 3.");
        }
    }
}
