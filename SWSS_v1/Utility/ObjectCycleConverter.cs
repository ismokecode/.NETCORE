using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWSS_v1.Utility
{
    public class ObjectCycleConverter<T> : JsonConverter<T>
    {
        private readonly Dictionary<string, T> _referenceCache = new Dictionary<string, T>();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T).IsAssignableFrom(typeToConvert);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var referenceId = GetReferenceId(value);
            if (!string.IsNullOrEmpty(referenceId))
            {
                writer.WriteStringValue(referenceId);
                return;
            }

            referenceId = Guid.NewGuid().ToString();
            _referenceCache.Add(referenceId, value);

            writer.WriteStartObject();

            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Name);
                var propertyValue = property.GetValue(value);
                JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
            }

            writer.WriteEndObject();
        }

        private string GetReferenceId(T value)
        {
            foreach (var kvp in _referenceCache)
            {
                if (ReferenceEquals(kvp.Value, value))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }
}
