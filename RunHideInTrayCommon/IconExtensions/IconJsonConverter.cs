using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconExtensions;

public class IconJsonConverter : JsonConverter<Icon>
{
    public override void Write(Utf8JsonWriter writer, Icon value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        using MemoryStream memoryStream = new();
        value.Save(memoryStream);
        writer.WriteBase64String("Data", memoryStream.ToArray());

        writer.WritePropertyName("Size");
        JsonSerializer.Serialize(writer, value.Size, options);

        writer.WriteEndObject();
    }
    public override Icon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        if (!(reader.Read() && reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "Data"))
            throw new JsonException();
        if (!reader.Read())
            throw new JsonException();
        using MemoryStream memoryStream = new(reader.GetBytesFromBase64());

        if (!(reader.Read() && reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "Size"))
            throw new JsonException();
        if (!reader.Read())
            throw new JsonException();
        Size size = JsonSerializer.Deserialize<Size>(ref reader, options);
        
        if (!(reader.Read() && reader.TokenType == JsonTokenType.EndObject))
            throw new JsonException();

        return new(memoryStream, size);
    }
}
