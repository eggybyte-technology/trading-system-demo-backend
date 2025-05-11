using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace CommonLib.Services
{
    /// <summary>
    /// JSON converter for MongoDB ObjectId
    /// </summary>
    public class ObjectIdConverter : JsonConverter<ObjectId>
    {
        /// <summary>
        /// Reads ObjectId from JSON
        /// </summary>
        public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string for ObjectId, got {reader.TokenType}");
            }

            string? value = reader.GetString();

            if (string.IsNullOrEmpty(value))
            {
                return ObjectId.Empty;
            }

            if (ObjectId.TryParse(value, out ObjectId objectId))
            {
                return objectId;
            }

            throw new JsonException($"Invalid ObjectId format: {value}");
        }

        /// <summary>
        /// Writes ObjectId to JSON
        /// </summary>
        public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// Service extension methods for MongoDB and JSON configuration
    /// </summary>
    public static class MongoDbServiceExtensions
    {
        /// <summary>
        /// Configures JSON options for ObjectId serialization/deserialization
        /// </summary>
        public static JsonSerializerOptions ConfigureMongoDbJsonOptions(this JsonSerializerOptions options)
        {
            options.Converters.Add(new ObjectIdConverter());
            return options;
        }
    }
}