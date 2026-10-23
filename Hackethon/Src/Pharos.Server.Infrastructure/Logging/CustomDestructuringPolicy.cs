using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Serilog.Core;
using Serilog.Events;
using Type = System.Type;

namespace Pharos.Server.Infrastructure.Logging;

internal class CustomDestructuringPolicy : IDestructuringPolicy
{
    private static readonly JsonSerializerOptions _grpcOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new GrpcTimestampToLocalStringConverter()
        }
    };


    private readonly JsonSerializerOptions _options;

    public CustomDestructuringPolicy(bool redactSensitiveData)
    {
        _options = CreateOptions(redactSensitiveData);
    }

    private static JsonSerializerOptions CreateOptions(bool redactSensitiveData)
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            TypeInfoResolver = redactSensitiveData
                ? new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        ConfigureRedactedProperties // gPRC payloads are not redacted (no sensitive information there), so we only apply this to non-gRPC payloads
                    }
                }
                : null,

            Converters =
            {
                new JsonStringEnumConverter(),
                new ObjectToStringJsonConverter<Encoding>(),
                new ObjectToStringJsonConverter<IPAddress>(),
                new ObjectToStringJsonConverter<StreamReader>(),
                new ObjectToStringJsonConverter<IntPtr>()
            }
        };
    }

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        try
        {
            var isGrpc = value is IMessage;
            using var jsonDoc = JsonSerializer.SerializeToDocument(value, isGrpc ? _grpcOptions : _options);
            result = ConvertElement(jsonDoc.RootElement, isGrpc);
        }
        catch
        {
            result = propertyValueFactory.CreatePropertyValue($"Failed to serialize {value.GetType().FullName} to JSON.");
        }

        return true;
    }

    private static void ConfigureRedactedProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.IsSensitive())
            {
                property.CustomConverter = (JsonConverter)Activator.CreateInstance(typeof(RedactingJsonConverter<>).MakeGenericType(property.PropertyType))!;
            }
        }
    }

    private static LogEventPropertyValue ConvertElement(JsonElement element, bool isGrpc)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var properties = element.EnumerateObject()
                        .Select(p => new LogEventProperty(p.Name, ConvertElement(p.Value, isGrpc)))
                        // Drop properties that resolved to null
                        .Where(p => p.Value is not ScalarValue sv || sv.Value is not null)
                        .ToList();

                    return new StructureValue(properties);
                }

            case JsonValueKind.Array:
                {
                    var items = element.EnumerateArray()
                        .Select(e => ConvertElement(e, isGrpc))
                        // Drop items that resolved to null
                        .Where(v => v is not ScalarValue sv || sv.Value is not null)
                        .ToList();

                    // If all items were dropped, treat this array as null (so [] disappears)
                    if (items.Count == 0)
                    {
                        return new ScalarValue(null);
                    }

                    return new SequenceValue(items);
                }

            case JsonValueKind.String:
                var str = element.GetString();
                // For gRPC payloads, treat empty strings as nulls so they are ignored by the null-filtering logic,
                // because strings are never null in IMessage.
                if (isGrpc && string.IsNullOrEmpty(str))
                {
                    return new ScalarValue(null);
                }

                return new ScalarValue(str);

            case JsonValueKind.Number:
                return element.TryGetInt64(out var l)
                    ? new ScalarValue(l)
                    : element.TryGetDouble(out var d)
                        ? new ScalarValue(d)
                        : new ScalarValue(element.GetRawText());

            case JsonValueKind.True:
                return new ScalarValue(true);

            case JsonValueKind.False:
                return new ScalarValue(false);

            case JsonValueKind.Null:
                return new ScalarValue(null);

            default:
                return new ScalarValue(element.GetRawText());
        }
    }

    internal class ObjectToStringJsonConverter<TValue> : JsonConverter<TValue>
    {
        public override TValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString());
        }
    }

    private sealed class RedactingJsonConverter<TValue> : JsonConverter<TValue>
    {
        public override TValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing redacted logging JSON is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options)
        {
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                writer.WriteStringValue(stringValue);
                return;
            }

            writer.WriteStringValue(PropertyRedactionExtensions.RedactedValue);
        }
    }

    internal sealed class GrpcTimestampToLocalStringConverter : JsonConverter<Timestamp>
    {
        public override Timestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Reading gRPC timestamps from log JSON is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options)
        {
            // Convert UTC timestamp to local time and write as a JSON string
            var local = value.ToDateTimeOffset().ToLocalTime();
            writer.WriteStringValue(local.ToString("O")); // ISO 8601, preserves precision
        }
    }
}