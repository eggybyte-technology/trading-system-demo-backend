using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Account transaction record
    /// </summary>
    public class Transaction : IndexedModel<Transaction>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "AccountDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Transactions";

        /// <summary>
        /// Account ID
        /// </summary>
        [BsonElement("accountId")]
        public ObjectId AccountId { get; set; }

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
        /// Transaction amount (positive for deposit, negative for withdrawal)
        /// </summary>
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Transaction type (deposit, withdrawal, trade, etc.)
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Transaction status (pending, completed, failed, etc.)
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Transaction creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Transaction completion time
        /// </summary>
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Reference ID (OrderId, WithdrawalId, etc.)
        /// </summary>
        [BsonElement("reference")]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Transaction>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Transaction>, bool>>
            {
                new Tuple<IndexKeysDefinition<Transaction>, bool>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Transaction>, bool>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.Type),
                    false
                )
            };
        }
    }
}