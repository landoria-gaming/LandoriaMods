using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    internal static class InventoryEventSender
    {
        private static ZRpc connection;
        private static long character;
        internal static string Stream;
        internal static bool EventsEnabled = true;

        internal static void Prepare(ZRpc rpc, long id)
        {
            if (connection == rpc && character == id) return;
            connection = rpc;
            character = id;
            Stream = Guid.NewGuid().ToString("D");
        }

        internal static void Send(Player player, JObject entry)
        {
            if (!EventsEnabled) return;
            var peer = ZNet.instance?.GetServerPeer();
            if (peer == null || !peer.m_rpc.IsConnected()) return;
            Prepare(peer.m_rpc, player.GetPlayerID());
            entry["eventId"] = Guid.NewGuid().ToString("D");
            entry["clientCapturedUtc"] = DateTime.UtcNow;
            entry["streamId"] = Stream;
            entry["contextSource"] = "client_reported";
            var package = new ZPackage();
            package.Write(entry.ToString(Formatting.None));
            if (package.Size() > InventoryWire.MaximumBytes)
                throw new InvalidDataException("Inventory event exceeds the byte limit.");
            peer.m_rpc.Invoke(InventoryProtocol.Event, player.GetPlayerID(), package);
        }

        internal static JArray Changes(Player player, ZPackage before)
            => InventoryChanges.Compare(InventoryWire.Read(new ZPackage(before.GetArray()), player.GetPlayerID()),
                InventoryWire.Read(new ZPackage(InventorySnapshotCapture.Capture(player).GetArray()), player.GetPlayerID()));

        internal static JObject Item(ItemDrop.ItemData item, int quantity)
            => new JObject { ["prefabHash"] = item.m_dropPrefab.name.GetStableHashCode(),
                ["prefabName"] = item.m_dropPrefab.name, ["quantity"] = quantity,
                ["quality"] = item.m_quality, ["variant"] = item.m_variant, ["worldLevel"] = item.m_worldLevel };

    }
}
