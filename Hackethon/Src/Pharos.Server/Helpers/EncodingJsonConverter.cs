using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pharos.Server.Helpers;

internal class EncodingJsonConverter : JsonConverter<Encoding>
{
    public override Encoding Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var encodingName = reader.GetString()!;
        return Encoding.GetEncoding(encodingName);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Encoding value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.WebName);
    }
}