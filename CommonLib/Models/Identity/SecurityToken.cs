using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Identity
{
    /// <summary>
    /// Security token (access token, refresh token)
    /// </summary>
    public class SecurityToken : IndexedModel<SecurityToken>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "IdentityDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "SecurityTokens";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Token value
        /// </summary>
        [BsonElement("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token type (refresh, access, etc.)
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = "access";

        /// <summary>
        /// Token creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Token expiration time
        /// </summary>
        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Is the token revoked
        /// </summary>
        [BsonElement("isRevoked")]
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// IP address used when token was created
        /// </summary>
        [BsonElement("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User agent used when token was created
        /// </summary>
        [BsonElement("userAgent")]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<SecurityToken>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<SecurityToken>, bool>>
            {
                new Tuple<IndexKeysDefinition<SecurityToken>, bool>(
                    Builders<SecurityToken>.IndexKeys.Ascending(t => t.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<SecurityToken>, bool>(
                    Builders<SecurityToken>.IndexKeys.Ascending(t => t.Token),
                    true
                ),
                new Tuple<IndexKeysDefinition<SecurityToken>, bool>(
                    Builders<SecurityToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                    false
                )
            };
        }
    }
}