using System;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.EventCollection
{
    internal static class ServerEventPublisher
    {
        private static long sequence;
        private static bool active;

        internal static void Open()
        {
            sequence = 0;
            active = true;
        }

        internal static bool Publish(object entry)
        {
            if (!active || entry == null || ZNet.instance == null || !ZNet.instance.IsServer())
                return false;
            var context = new JObject
            {
                ["schemaVersion"] = 1,
                ["code"] = "SERVER_EVENT_RECORDED",
                ["source"] = "server",
                ["receivedUtc"] = DateTime.UtcNow.ToString("O"),
                ["worldId"] = ZNet.instance.GetWorldUID().ToString(),
                ["serverSessionId"] = ZNet.GetUID().ToString(),
                ["serverSequence"] = ++sequence
            };
            return ReceivedJournal.Append(new Event { context = context, entry = entry });
        }

        internal static void Close() => active = false;
    }
}
