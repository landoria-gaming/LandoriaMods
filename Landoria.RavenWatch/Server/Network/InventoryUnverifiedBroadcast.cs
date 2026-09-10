using System;
using System.Linq;
using Landoria.RavenWatch.Shared;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Network
{
    internal static class InventoryUnverifiedBroadcast
    {
        internal static void Send(JObject entry)
        {
            if (ZNet.instance == null || !ZNet.instance.IsDedicated()) return;
            string items = Describe(entry);
            if (string.IsNullOrEmpty(items)) return;
            string message = ((string)entry["playerName"] ?? "Unknown player") +
                ": unverified inventory event after 3 checks — " + items + ".";
            foreach (var peer in ZNet.instance.GetPeers().Where(p => p.IsReady() && p.m_rpc.IsConnected()))
            {
                try { peer.m_rpc.Invoke(InventoryProtocol.Unverified, message); }
                catch (Exception error) { RavenWatchLog.Log.LogError(error); }
            }
        }

        private static string Describe(JObject entry)
        {
            if (entry["changes"] is JArray changes && changes.Count > 0)
                return string.Join(", ", changes.Select(item => Item(item, "quantityDelta")));
            if (entry["craftedItem"] is JObject crafted) return "crafted " + Item(crafted, "quantity");
            if (entry["brokenItem"] is JObject broken) return "broken " + Item(broken, "quantity");
            return entry["prefabName"] == null ? null : Item(entry, "quantityDelta");
        }
        private static string Item(JToken item, string quantity)
            => ((long?)item[quantity] ?? 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " × " + ((string)item["prefabName"] ?? "unknown item");
    }
}
