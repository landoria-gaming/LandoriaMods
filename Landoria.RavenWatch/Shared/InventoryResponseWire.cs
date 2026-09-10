using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Shared
{
    internal static class InventoryResponseWire
    {
        internal static JObject Read(ZPackage package, long id)
        {
            if (package.Size() > InventoryWire.MaximumBytes + 256)
                throw new InvalidDataException("Inventory response too large.");
            string request = package.ReadString(), stream = package.ReadString();
            var snapshot = InventoryWire.Read(package.ReadPackage(), id);
            if (package.GetPos() != package.Size()
                || !Guid.TryParse(request, out _) || !Guid.TryParse(stream, out _))
                throw new InvalidDataException("Invalid inventory response.");
            snapshot["requestId"] = request;
            snapshot["streamId"] = stream;
            snapshot["context"] = "inventory_snapshot";
            snapshot["contextSource"] = "server_requested";
            return snapshot;
        }
    }
}
