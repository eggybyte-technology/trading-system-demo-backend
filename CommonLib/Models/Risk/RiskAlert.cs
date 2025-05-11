using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// Risk alert
    /// </summary>
    public class RiskAlert : IndexedModel<RiskAlert>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "RiskDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "RiskAlerts";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Alert severity (low, medium, high, critical)
        /// </summary>
        [BsonElement("severity")]
        public string Severity { get; set; } = "medium";

        /// <summary>
        /// Alert type
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Alert message
        /// </summary>
        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Related order ID
        /// </summary>
        [BsonElement("orderId")]
        public ObjectId? OrderId { get; set; }

        /// <summary>
        /// Related transaction ID
        /// </summary>
        [BsonElement("transactionId")]
        public ObjectId? TransactionId { get; set; }

        /// <summary>
        /// Whether the alert is acknowledged
        /// </summary>
        [BsonElement("isAcknowledged")]
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// Time when the alert was acknowledged
        /// </summary>
        [BsonElement("acknowledgedAt")]
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<RiskAlert>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<RiskAlert>, bool>>
            {
                new Tuple<IndexKeysDefinition<RiskAlert>, bool>(
                    Builders<RiskAlert>.IndexKeys.Ascending(a => a.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<RiskAlert>, bool>(
                    Builders<RiskAlert>.IndexKeys.Descending(a => a.CreatedAt),
                    false
                )
            };
        }
    }
}