using System;
using System.Security;

namespace Landoria.Socialize
{
    internal static class ChatFormattingPolicy
    {
        private const string GroupColor = "#4A90E2";
        private const string PrivateColor = "#2FAE5F";
        private const string ShoutColor = "#FFFF00";

        internal static string FormatGroup(string sender, string message) =>
            "<color=" + GroupColor + ">" + sender + ": " + message + "</color>";

        internal static string FormatPrivate(string user, string text)
        {
            string separator = (text ?? "").StartsWith("to ", StringComparison.OrdinalIgnoreCase)
                ? " "
                : ": ";
            return "<color=" + PrivateColor + ">" + user + separator + text + "</color>";
        }

        internal static string FormatShout(string user, string text) =>
            "<color=orange>" + user + "</color>: <color=" + ShoutColor + ">" +
            text + "</color>";

        internal static string FormatArrival(string playerName, string message) =>
            "<color=orange>" + SecurityElement.Escape(playerName) + "</color>" +
            "<color=white>: </color><color=" + ShoutColor + ">" +
            SecurityElement.Escape(message) + "</color>";

        internal static string FormatPing(string user, string target, string message)
        {
            string recipient = string.IsNullOrEmpty(target) ? ": " : " to " + target + ": ";
            return "<color=" + PrivateColor + ">" + user + recipient + "</color>" +
                   "<color=" + ShoutColor + ">((Ping))</color>" +
                   "<color=" + PrivateColor + "> " + message + "</color>";
        }
    }
}
