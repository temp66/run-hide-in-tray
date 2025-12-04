using Json;
using RunHideInTrayCommon.Windows;

using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconExtensions;

internal class IconJsonConverter : JsonConverter<Icon>
{
    public override void Write(Utf8JsonWriter writer, Icon value, JsonSerializerOptions options)
    {
        // `Icon.Save` gives only 16 colors if the icon is created from handle!
        // https://www.vbforums.com/showthread.php?395933-2003-Icon-only-saved-as-16-colors
        // https://forum.codeproject.com/topic/97364/icon-image-loses-quality-after-deserialization
        // https://stackoverflow.com/questions/10844344/net-save-icon-with-higher-quality
        // https://stackoverflow.com/questions/52689421/converting-icon-object-to-and-from-byte-causes-quality-loss
        // https://stackoverflow.com/questions/2289894/how-can-i-save-hicon-to-an-ico-file#comment16131406_4338491

        writer.WriteStartObject();

        writer.WritePropertyName("HICON");
        if (Icon.GetIconData(value) is not null)
        {
            writer.WriteBooleanValue(false);

            using MemoryStream memoryStream = new();
            value.Save(memoryStream);
            writer.WriteBase64String("Data", memoryStream.ToArray());

            writer.WritePropertyName("Size");
            JsonSerializer.Serialize(writer, value.Size, options);
        }
        else
        {
            writer.WriteBooleanValue(true);

            using Bitmap bitmap = Bitmap.FromHicon(value.Handle);
            using MemoryStream memoryStream = new();
            // There is no encoder for icon.
            // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/using-image-encoders-and-decoders-in-managed-gdi
            // bitmap.Save(memoryStream, ImageFormat.Icon);
            bitmap.Save(memoryStream, ImageFormat.Png);
            writer.WriteBase64String("Bitmap", memoryStream.ToArray());
        }

        writer.WriteEndObject();
    }
    public override Icon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        reader.ReadPropertyName("HICON");
        if (!reader.ReadBoolean())
        {
            reader.ReadPropertyName("Data");
            using MemoryStream memoryStream = new(reader.ReadBytesFromBase64());

            reader.ReadPropertyName("Size");
            if (!reader.Read())
                throw new JsonException();
            Size size = JsonSerializer.Deserialize<Size>(ref reader, options);

            reader.ReadEndObject();

            return new(memoryStream, size);
        }
        else
        {
            reader.ReadPropertyName("Bitmap");
            using MemoryStream memoryStream = new(reader.ReadBytesFromBase64());
            using Bitmap bitmap = new(memoryStream);
            nint iconHandle = bitmap.GetHicon();
            using SafeIconHandle iconHandle_ = new(iconHandle);
            using Icon icon = Icon.FromHandle(iconHandle);

            reader.ReadEndObject();

            return new(icon, icon.Size);
        }
    }
}
