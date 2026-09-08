using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal static class EventDeserializer
    {
        private static readonly IReadOnlyDictionary<string, Type> eventTypes =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                ["PLAYER_APPEARANCE_OBSERVED"] = typeof(PlayerAppearanceObserved),
                ["PLAYER_DEBUG_FLY_OBSERVED"] = typeof(PlayerDebugFlyObserved),
                ["CREATURE_APPEARANCE_OBSERVED"] = typeof(CreatureAppearanceObserved),
                ["CONTAINER_OPEN_OBSERVED"] = typeof(ContainerObservation),
                ["GROUND_ITEM_OBSERVED"] = typeof(ItemObservation),
                ["JOURNAL_OVERFLOW"] = typeof(JournalOverflow)
            };

        internal static bool TryDeserialize(string json, int index, out object entry)
        {
            entry = null;
            try
            {
                JObject payload = JObject.Parse(json);
                string code = (string)payload["code"];
                string kind = (string)payload["kind"];
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(kind) ||
                    !string.Equals(code, kind.ToUpperInvariant(), StringComparison.Ordinal))
                    throw new JsonSerializationException("Event code and kind are missing or inconsistent.");
                if (!eventTypes.TryGetValue(code, out Type type))
                    throw new JsonSerializationException("Unsupported event code '" + code + "'.");
                entry = JournalJson.Deserialize(payload, type)
                    ?? throw new JsonSerializationException("Deserialization returned null.");
                return true;
            }
            catch (Exception exception)
            {
                RavenWatchPlugin.Log.LogError("Ignored invalid observer event #" + index + ": " + exception);
                return false;
            }
        }
    }
}
