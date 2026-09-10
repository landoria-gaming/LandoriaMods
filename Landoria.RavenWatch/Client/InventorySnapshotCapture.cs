using System;
using System.IO;
using Landoria.RavenWatch.Shared;

namespace Landoria.RavenWatch.Client
{
    internal static class InventorySnapshotCapture
    {
        internal static ZPackage Capture(Player player)
        {
            var inventory = player.GetInventory();
            if (inventory.GetAllItems().Count > InventoryWire.MaximumItems)
                throw new InvalidDataException("Inventory exceeds the item limit.");
            var package = new ZPackage();
            package.Write(DateTime.UtcNow.Ticks);
            inventory.Save(package);
            if (package.Size() > InventoryWire.MaximumBytes)
                throw new InvalidDataException("Inventory exceeds the byte limit.");
            return package;
        }
    }
}
