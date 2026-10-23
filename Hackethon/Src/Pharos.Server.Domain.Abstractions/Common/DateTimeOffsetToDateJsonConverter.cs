using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pharos.Server.Domain.Common
{
    public class DateTimeOffsetToDateJsonConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime().Date;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Date);
        }
    }
}
