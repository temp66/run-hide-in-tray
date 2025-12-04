using System.Text.Json;

namespace Json;

internal static class Utf8JsonReaderExtensions
{
    extension(ref Utf8JsonReader reader)
    {
        public void ReadEndObject()
        {
            if (!(reader.Read() && reader.TokenType == JsonTokenType.EndObject))
                throw new JsonException();
        }

        public void ReadPropertyName(string propertyName)
        {
            if (!(reader.Read() && reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == propertyName))
                throw new JsonException();
        }

        public bool ReadBoolean()
        {
            if (!reader.Read())
                throw new JsonException();
            return reader.GetBoolean();
        }

        public byte[] ReadBytesFromBase64()
        {
            if (!reader.Read())
                throw new JsonException();
            return reader.GetBytesFromBase64();
        }
    }
}
