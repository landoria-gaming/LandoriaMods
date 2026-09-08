using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Landoria.RavenWatch
{
    internal static class JournalJson
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters = { new PositionConverter() },
            TypeNameHandling = TypeNameHandling.None
        };

        internal static string Serialize(object entry)
        {
            return JsonConvert.SerializeObject(entry, Formatting.None, settings);
        }

        internal static object Deserialize(JObject payload, Type type)
        {
            return payload.ToObject(type, JsonSerializer.Create(settings));
        }

        internal static string SerializeEvent(object entry)
        {
            return CreateEvent(entry).ToString(Formatting.None);
        }

        internal static JObject CreateEvent(object entry)
        {
            JObject payload = JObject.FromObject(entry, JsonSerializer.Create(settings));
            string kind = (string)payload["kind"];
            if (string.IsNullOrWhiteSpace(kind)) throw new InvalidOperationException("Journal event kind is required.");
            payload["code"] = kind.ToUpperInvariant();
            return payload;
        }

        private sealed class PositionConverter : JsonConverter
        {
            public override bool CanRead => true;

            public override bool CanConvert(Type objectType)
                => objectType == typeof(Vector3) || Nullable.GetUnderlyingType(objectType) == typeof(Vector3);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }
                Vector3 position = (Vector3)value;
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(position.x);
                writer.WritePropertyName("y");
                writer.WriteValue(position.y);
                writer.WritePropertyName("z");
                writer.WriteValue(position.z);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType,
                object existingValue, JsonSerializer serializer)
            {
                bool nullable = Nullable.GetUnderlyingType(objectType) == typeof(Vector3);
                if (reader.TokenType == JsonToken.Null)
                {
                    if (nullable) return null;
                    throw new JsonSerializationException("A required position is null.");
                }
                JObject value = JObject.Load(reader);
                return new Vector3(ReadCoordinate(value, "x"), ReadCoordinate(value, "y"),
                    ReadCoordinate(value, "z"));
            }

            private static float ReadCoordinate(JObject value, string name)
            {
                JToken coordinate = value[name];
                if (coordinate == null || (coordinate.Type != JTokenType.Float &&
                    coordinate.Type != JTokenType.Integer))
                    throw new JsonSerializationException("Position coordinate '" + name + "' is invalid.");
                return coordinate.Value<float>();
            }
        }
    }
}
