using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Notification
{
    /// <summary>
    /// User notification
    /// </summary>
    public class Notification : IndexedModel<Notification>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "NotificationDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Notifications";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Notification type
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Notification title
        /// </summary>
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notification message
        /// </summary>
        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Whether the notification is read
        /// </summary>
        [BsonElement("isRead")]
        public bool IsRead { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Read time
        /// </summary>
        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Related entity ID (OrderId, TransactionId, etc.)
        /// </summary>
        [BsonElement("relatedId")]
        public string RelatedId { get; set; } = string.Empty;

        /// <summary>
        /// Additional data in JSON format
        /// </summary>
        [BsonElement("data")]
        public string Data { get; set; } = "{}";

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Notification>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Notification>, bool>>
            {
                new Tuple<IndexKeysDefinition<Notification>, bool>(
                    Builders<Notification>.IndexKeys.Ascending(n => n.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Notification>, bool>(
                    Builders<Notification>.IndexKeys.Descending(n => n.CreatedAt),
                    false
                )
            };
        }
    }
}