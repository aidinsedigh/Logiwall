using System.Text.Json;

namespace Logiwall.Shared.Utils;
public class ObjectToStringConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int i))
                    return i;
                if (reader.TryGetInt64(out long l))
                    return l;
                
                return reader.GetDouble();
            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean();
            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
