using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonLib.Models
{
    /// <summary>
    /// Base class for all business models, providing mapping with MongoDB databases and collections
    /// </summary>
    public abstract class BaseModel
    {
        /// <summary>
        /// Record ID
        /// </summary>
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Gets the database name for this model
        /// </summary>
        public abstract string Database { get; }

        /// <summary>
        /// Gets the collection name for this model
        /// </summary>
        public abstract string Collection { get; }
    }
}