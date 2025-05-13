using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Represents a trading order - the core entity of the Trading domain
    /// </summary>
    public class Order : IndexedModel<Order>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "TradingDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Orders";

        /// <summary>
        /// ID of the user who placed this order
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Symbol name for the order (e.g., "BTC-USDT")
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Order side (BUY, SELL)
        /// </summary>
        [BsonElement("side")]
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Order type (LIMIT, MARKET, etc.)
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Order status (NEW, PARTIALLY_FILLED, FILLED, CANCELED, REJECTED)
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "NEW";

        /// <summary>
        /// Time in force policy (GTC, IOC, FOK)
        /// </summary>
        [BsonElement("timeInForce")]
        public string TimeInForce { get; set; } = "GTC";

        /// <summary>
        /// Order price (required for LIMIT orders)
        /// </summary>
        [BsonElement("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Original quantity specified
        /// </summary>
        [BsonElement("originalQuantity")]
        public decimal OriginalQuantity { get; set; }

        /// <summary>
        /// Quantity that has been executed
        /// </summary>
        [BsonElement("executedQuantity")]
        public decimal ExecutedQuantity { get; set; }

        /// <summary>
        /// Stop price for STOP_LOSS and STOP_LOSS_LIMIT orders
        /// </summary>
        [BsonElement("stopPrice")]
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// Visible quantity for ICEBERG orders
        /// </summary>
        [BsonElement("icebergQuantity")]
        public decimal? IcebergQuantity { get; set; }

        /// <summary>
        /// When the order was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the order was last updated
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Whether the order is working (active in order book)
        /// </summary>
        [BsonElement("isWorking")]
        public bool IsWorking { get; set; } = true;

        /// <summary>
        /// Whether the order is locked for matching process
        /// </summary>
        [BsonElement("isLocked")]
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// Time when the order was locked
        /// </summary>
        [BsonElement("lockedAt")]
        public DateTime? LockedAt { get; set; }

        /// <summary>
        /// Unique ID for the lock
        /// </summary>
        [BsonElement("lockId")]
        public string? LockId { get; set; }

        /// <summary>
        /// Expiration time for the lock
        /// </summary>
        [BsonElement("lockExpiration")]
        public DateTime? LockExpiration { get; set; }

        /// <summary>
        /// ID of the matching job that locked this order
        /// </summary>
        [BsonElement("lockedByJobId")]
        public ObjectId? LockedByJobId { get; set; }

        /// <summary>
        /// Cumulative amount in quote asset
        /// </summary>
        [BsonElement("cumulativeQuoteQuantity")]
        public decimal CumulativeQuoteQuantity { get; set; }

        /// <summary>
        /// List of trades executed against this order
        /// </summary>
        [BsonElement("trades")]
        public List<Trade> Trades { get; set; } = new();

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Order>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Order>, bool>>
            {
                new Tuple<IndexKeysDefinition<Order>, bool>(
                    Builders<Order>.IndexKeys.Ascending(o => o.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Order>, bool>(
                    Builders<Order>.IndexKeys.Ascending(o => o.Symbol),
                    false
                ),
                new Tuple<IndexKeysDefinition<Order>, bool>(
                    Builders<Order>.IndexKeys.Ascending(o => o.Status),
                    false
                )
            };
        }
    }
}