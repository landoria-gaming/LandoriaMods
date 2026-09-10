using UnityEngine;
using UnityEngine.Rendering;

namespace Landoria.ServerInventory.Server
{
    internal static class ServerUi
    {
        // Also works in the startup scene, before ZNet exists.
        internal static bool Disabled => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    }
}
