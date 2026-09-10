using Landoria.RavenWatch.Shared;
using Landoria.RavenWatch.Server.Network;
using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Journal
{
    internal static class InventoryRpcLog
    {
        internal static JObject Decode(byte[] bytes, bool incoming, bool debug)
        {
            if (bytes.Length < 4) return null;
            int hash = BitConverter.ToInt32(bytes, 0);
            string name = hash == InventoryProtocol.Event.GetStableHashCode() ? InventoryProtocol.Event
                : hash == InventoryProtocol.Request.GetStableHashCode() ? InventoryProtocol.Request
                : hash == InventoryProtocol.Response.GetStableHashCode() ? InventoryProtocol.Response : null;
            if (name == null) return null;
            if (bytes.Length > InventoryWire.MaximumBytes + 512)
                throw new InvalidDataException("Inventory RPC exceeds the byte limit.");
            var package = new ZPackage(bytes);
            package.ReadInt();
            string wireName = debug ? package.ReadString() : null;
            JObject result;
            if (name == InventoryProtocol.Request) result = new JObject { ["requestId"] = package.ReadString() };
            else
            {
                long id = package.ReadLong();
                result = name == InventoryProtocol.Event ? InventoryEventReceiver.Read(package.ReadPackage())
                    : InventoryResponseWire.Read(package.ReadPackage(), id);
            }
            result["name"] = name;
            result["wireName"] = wireName;
            result["direction"] = incoming ? "client_to_server" : "server_to_client";
            result["decodeStatus"] = "decoded";
            if (package.GetPos() != package.Size()) throw new InvalidDataException("Trailing inventory RPC bytes.");
            return result;
        }
    }
}
