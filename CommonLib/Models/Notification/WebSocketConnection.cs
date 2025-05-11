using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Notification
{
    /// <summary>
    /// WebSocket connection for real-time updates
    /// </summary>
    public class WebSocketConnection : IndexedModel<WebSocketConnection>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "NotificationDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "WebSocketConnections";

        /// <summary>
        /// Connection ID
        /// </summary>
        [BsonElement("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// User ID (null for anonymous connections)
        /// </summary>
        [BsonElement("userId")]
        public ObjectId? UserId { get; set; }

        /// <summary>
        /// IP address
        /// </summary>
        [BsonElement("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User agent
        /// </summary>
        [BsonElement("userAgent")]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Connection establishment time
        /// </summary>
        [BsonElement("connectedAt")]
        public DateTime ConnectedAt { get; set; }

        /// <summary>
        /// Last activity time
        /// </summary>
        [BsonElement("lastActivityAt")]
        public DateTime LastActivityAt { get; set; }

        /// <summary>
        /// Subscribed data channels
        /// </summary>
        [BsonElement("subscribedChannels")]
        public List<string> SubscribedChannels { get; set; } = new();

        /// <summary>
        /// Subscribed symbols
        /// </summary>
        [BsonElement("subscribedSymbols")]
        public List<string> SubscribedSymbols { get; set; } = new();

        /// <summary>
        /// Whether connection is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<WebSocketConnection>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<WebSocketConnection>, bool>>
            {
                new Tuple<IndexKeysDefinition<WebSocketConnection>, bool>(
                    Builders<WebSocketConnection>.IndexKeys.Ascending(w => w.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<WebSocketConnection>, bool>(
                    Builders<WebSocketConnection>.IndexKeys.Ascending(w => w.ConnectionId),
                    true
                )
            };
        }
    }
}