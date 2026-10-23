using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pharos.Server.Settings.Infrastructure.Serialization;

public class TypeToStringConverter : JsonConverter<Type?>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeName = reader.GetString();
        if (string.IsNullOrEmpty(typeName))
        {
            return null;
        }

        return Type.GetType(typeName);
    }

    public override void Write(Utf8JsonWriter writer, Type? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Name);
    }
}
