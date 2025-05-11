using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Represents the order matching configuration for a specific symbol
    /// </summary>
    public class OrderMatcher : IndexedModel<OrderMatcher>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "TradingDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "OrderMatchers";

        /// <summary>
        /// Symbol this matcher is responsible for (e.g., "BTC-USDT")
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Last time orders were matched for this symbol
        /// </summary>
        [BsonElement("lastMatchTime")]
        public DateTime LastMatchTime { get; set; }

        /// <summary>
        /// Is this matcher actively running
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Match frequency in milliseconds
        /// </summary>
        [BsonElement("matchFrequencyMs")]
        public int MatchFrequencyMs { get; set; } = 1000;

        /// <summary>
        /// Maximum number of orders to match in one batch
        /// </summary>
        [BsonElement("batchSize")]
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Last processed order ID
        /// </summary>
        [BsonElement("lastProcessedOrderId")]
        public ObjectId? LastProcessedOrderId { get; set; }

        /// <summary>
        /// Statistics about matching performance
        /// </summary>
        [BsonElement("stats")]
        public MatcherStatistics Stats { get; set; } = new MatcherStatistics();

        /// <summary>
        /// Whether the matcher is enabled
        /// </summary>
        [BsonElement("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Matching interval in milliseconds
        /// </summary>
        [BsonElement("matchingIntervalMs")]
        public int MatchingIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Maximum orders to match per interval
        /// </summary>
        [BsonElement("maxOrdersPerInterval")]
        public int MaxOrdersPerInterval { get; set; } = 1000;

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Last run time
        /// </summary>
        [BsonElement("lastRunAt")]
        public DateTime? LastRunAt { get; set; }

        /// <summary>
        /// Currently running job ID
        /// </summary>
        [BsonElement("currentJobId")]
        public ObjectId? CurrentJobId { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<OrderMatcher>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<OrderMatcher>, bool>>
            {
                new Tuple<IndexKeysDefinition<OrderMatcher>, bool>(
                    Builders<OrderMatcher>.IndexKeys.Ascending(o => o.Symbol),
                    true
                )
            };
        }
    }

    /// <summary>
    /// Statistics about the order matcher performance
    /// </summary>
    public class MatcherStatistics
    {
        /// <summary>
        /// Total number of orders processed
        /// </summary>
        [BsonElement("totalOrdersProcessed")]
        public long TotalOrdersProcessed { get; set; }

        /// <summary>
        /// Total number of trades generated
        /// </summary>
        [BsonElement("totalTradesGenerated")]
        public long TotalTradesGenerated { get; set; }

        /// <summary>
        /// Average matching time in milliseconds
        /// </summary>
        [BsonElement("averageMatchTimeMs")]
        public double AverageMatchTimeMs { get; set; }

        /// <summary>
        /// Last match batch execution time in milliseconds
        /// </summary>
        [BsonElement("lastMatchTimeMs")]
        public double LastMatchTimeMs { get; set; }
    }
}