using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Order matching job
    /// </summary>
    public class MatchingJob : IndexedModel<MatchingJob>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "TradingDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "MatchingJobs";

        /// <summary>
        /// Symbol name
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Job status (pending, running, completed, failed)
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Order IDs being processed
        /// </summary>
        [BsonElement("orderIds")]
        public List<ObjectId> OrderIds { get; set; } = new();

        /// <summary>
        /// Created trades IDs
        /// </summary>
        [BsonElement("tradeIds")]
        public List<ObjectId> TradeIds { get; set; } = new();

        /// <summary>
        /// Number of orders processed
        /// </summary>
        [BsonElement("ordersProcessed")]
        public int OrdersProcessed { get; set; }

        /// <summary>
        /// Number of trades created
        /// </summary>
        [BsonElement("tradesCreated")]
        public int TradesCreated { get; set; }

        /// <summary>
        /// Number of trades generated (alias for TradesCreated)
        /// </summary>
        [BsonElement("tradesGenerated")]
        public int TradesGenerated { get; set; }

        /// <summary>
        /// Total volume of trades
        /// </summary>
        [BsonElement("totalVolume")]
        public decimal TotalVolume { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        [BsonElement("processingTimeMs")]
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        [BsonElement("startedAt")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<MatchingJob>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<MatchingJob>, bool>>
            {
                new Tuple<IndexKeysDefinition<MatchingJob>, bool>(
                    Builders<MatchingJob>.IndexKeys.Ascending(j => j.Symbol),
                    false
                ),
                new Tuple<IndexKeysDefinition<MatchingJob>, bool>(
                    Builders<MatchingJob>.IndexKeys.Descending(j => j.CreatedAt),
                    false
                )
            };
        }
    }
}