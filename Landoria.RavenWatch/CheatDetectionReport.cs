namespace Landoria.RavenWatch
{
    public sealed class CheatDetectionReport
    {
        public string DetectedUtc { get; }
        public string DetectionId { get; }
        public string DetectionCode { get; }
        public int AnomalyConfidence { get; }
        public int AttributionConfidence { get; }
        public string SuspectedPlayerName { get; }
        public string SuspectedSessionId { get; }
        public string Anomaly { get; }
        public string Explanation { get; }
        public string Json { get; }
        public bool SuppressChat { get; set; }

        internal CheatDetectionReport(string detectedUtc, string detectionId,
            string detectionCode, int anomalyConfidence, int attributionConfidence,
            string suspectedPlayerName,
            string suspectedSessionId, string anomaly, string explanation, string json)
        {
            DetectedUtc = detectedUtc;
            DetectionId = detectionId;
            DetectionCode = detectionCode;
            AnomalyConfidence = anomalyConfidence;
            AttributionConfidence = attributionConfidence;
            SuspectedPlayerName = suspectedPlayerName;
            SuspectedSessionId = suspectedSessionId;
            Anomaly = anomaly;
            Explanation = explanation;
            Json = json;
        }
    }
}
