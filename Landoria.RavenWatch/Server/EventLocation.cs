using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch.Server
{
    internal static class EventLocation
    {
        internal static Vector3 Read(JToken position) => new Vector3(
            (float)position["x"], (float)position["y"], (float)position["z"]);

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
