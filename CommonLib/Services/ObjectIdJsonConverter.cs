using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace CommonLib.Services
{
    public class ObjectIdJsonConverter : JsonConverter<ObjectId>
    {
        public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string value for ObjectId");
            }

            string? value = reader.GetString();
            return string.IsNullOrEmpty(value) ? ObjectId.Empty : ObjectId.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}