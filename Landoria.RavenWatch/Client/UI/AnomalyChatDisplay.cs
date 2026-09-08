using HarmonyLib;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal static class AnomalyChatDisplay
    {
        private const float DisplaySeconds = 10f;
        private static float visibleUntil;
        internal static bool ShouldStayVisible => Time.realtimeSinceStartup < visibleUntil;

        internal static void Show(string playerName, string anomaly)
        {
            Chat chat = Chat.instance;
            if (chat == null) return;
            string safeName = Sanitize(playerName, "unknown player");
            string safeAnomaly = Sanitize(anomaly, "An anomaly was detected.");
            chat.AddString("<color=#FF3B30><b>Suspected cheating:</b></color> " +
                "<color=#FFD700>" + safeName + "</color> " +
                "<color=#FF3B30>" + safeAnomaly + "</color>");
            visibleUntil = Time.realtimeSinceStartup + DisplaySeconds;
            chat.m_chatWindow.gameObject.SetActive(true);
        }

        private static string Sanitize(string value, string fallback)
        {
            return (string.IsNullOrWhiteSpace(value) ? fallback : value)
                .Replace('<', ' ').Replace('>', ' ');
        }

    }

    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    internal static class AnomalyChatVisibilityPatch
    {
        private static void Prefix(ref float ___m_hideTimer)
        {
            if (AnomalyChatDisplay.ShouldStayVisible) ___m_hideTimer = 0f;
        }
    }
}
