using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RunHideInTrayCommon.Json;

public class IconJsonConverter : JsonConverter<Icon>
{
    public override Icon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new(new MemoryStream(reader.GetBytesFromBase64()));
    }

    public override void Write(Utf8JsonWriter writer, Icon value, JsonSerializerOptions options)
    {
        using MemoryStream memoryStream = new();
        value.Save(memoryStream);
        writer.WriteBase64StringValue(memoryStream.ToArray());
    }
}
