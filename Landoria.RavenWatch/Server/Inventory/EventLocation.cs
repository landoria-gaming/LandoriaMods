using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server.Inventory
{
    internal static class EventLocation
    {
        internal static void Add(JObject entry, Vector3? position)
        {
            entry["x"] = position?.x;
            entry["y"] = position?.y;
            entry["z"] = position?.z;
            entry["biome"] = position.HasValue && WorldGenerator.instance != null
                ? WorldGenerator.instance.GetBiome(position.Value).ToString() : null;
        }
    }
}
