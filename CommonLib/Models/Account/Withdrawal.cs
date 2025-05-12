using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Withdrawal request model
    /// </summary>
    public class Withdrawal : IndexedModel<Withdrawal>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "AccountDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Withdrawals";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Asset type
        /// </summary>
        [BsonElement("asset")]
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Withdrawal amount
        /// </summary>
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Destination address
        /// </summary>
        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Optional memo/tag for certain assets
        /// </summary>
        [BsonElement("memo")]
        public string? Memo { get; set; }

        /// <summary>
        /// Withdrawal status (pending, completed, rejected, etc.)
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Completion time
        /// </summary>
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Withdrawal>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Withdrawal>, bool>>
            {
                new Tuple<IndexKeysDefinition<Withdrawal>, bool>(
                    Builders<Withdrawal>.IndexKeys.Ascending(w => w.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Withdrawal>, bool>(
                    Builders<Withdrawal>.IndexKeys.Ascending(w => w.Status),
                    false
                )
            };
        }
    }
}