namespace Landoria.RavenWatch
{
    public sealed class CheatDetectionReport
    {
        public string DetectedUtc { get; }
        public string DetectionId { get; }
        public string DetectionCode { get; }
        public int Confidence { get; }
        public string SuspectedPlayerName { get; }
        public string SuspectedSessionId { get; }
        public string Anomaly { get; }
        public string Explanation { get; }
        public string Json { get; }
        public bool SuppressChat { get; set; }

        internal CheatDetectionReport(string detectedUtc, string detectionId,
            string detectionCode, int confidence, string suspectedPlayerName,
            string suspectedSessionId, string anomaly, string explanation, string json)
        {
            DetectedUtc = detectedUtc;
            DetectionId = detectionId;
            DetectionCode = detectionCode;
            Confidence = confidence;
            SuspectedPlayerName = suspectedPlayerName;
            SuspectedSessionId = suspectedSessionId;
            Anomaly = anomaly;
            Explanation = explanation;
            Json = json;
        }
    }
}
