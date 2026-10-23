using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Application.AuditTrail
{
    public class AuditEventData
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Data { get; set; } = new();

        public void AddData<T>(EventDataFields key, T value)
        {
            ValidateDataType(key, typeof(T));
            var jsonString = JsonSerializer.Serialize(value);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
            Data[key.ToString()] = jsonElement;
        }

        private static void ValidateDataType(EventDataFields key, Type type)
        {
            var allowedType = GetAllowedType(key);
            if (allowedType != type && Nullable.GetUnderlyingType(type) != allowedType)
            {
                throw new ArgumentException($"Invalid type for field {key}. Expected {allowedType} but got {type}");
            }
        }

        private static Type GetAllowedType(EventDataFields key)
        {
            var memberInfo = typeof(EventDataFields).GetMember(key.ToString()).First();
            var attribute = memberInfo.GetCustomAttribute<AllowedTypeAttribute>();
            return attribute?.AllowedType ?? throw new ArgumentException($"No allowed type defined for key {key}");
        }

        public override string ToString()
        {
            var dataStrings = new List<string>();
            foreach (var kvp in Data)
            {
                dataStrings.Add($"{kvp.Key}: {kvp.Value}");
            }

            return string.Join(",", dataStrings);
        }
    }
}
