using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Trading pair symbol
    /// </summary>
    public class Symbol : IndexedModel<Symbol>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "MarketDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Symbols";

        /// <summary>
        /// Symbol name (e.g., "BTC-USDT")
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Base asset (e.g., "BTC")
        /// </summary>
        [BsonElement("baseAsset")]
        public string BaseAsset { get; set; } = string.Empty;

        /// <summary>
        /// Quote asset (e.g., "USDT")
        /// </summary>
        [BsonElement("quoteAsset")]
        public string QuoteAsset { get; set; } = string.Empty;

        /// <summary>
        /// Base asset precision
        /// </summary>
        [BsonElement("baseAssetPrecision")]
        public int BaseAssetPrecision { get; set; } = 8;

        /// <summary>
        /// Quote asset precision
        /// </summary>
        [BsonElement("quotePrecision")]
        public int QuotePrecision { get; set; } = 2;

        /// <summary>
        /// Minimum price
        /// </summary>
        [BsonElement("minPrice")]
        public decimal MinPrice { get; set; }

        /// <summary>
        /// Maximum price
        /// </summary>
        [BsonElement("maxPrice")]
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// Tick size - minimum price movement
        /// </summary>
        [BsonElement("tickSize")]
        public decimal TickSize { get; set; }

        /// <summary>
        /// Minimum quantity
        /// </summary>
        [BsonElement("minQty")]
        public decimal MinQty { get; set; }

        /// <summary>
        /// Maximum quantity
        /// </summary>
        [BsonElement("maxQty")]
        public decimal MaxQty { get; set; }

        /// <summary>
        /// Step size - minimum quantity movement
        /// </summary>
        [BsonElement("stepSize")]
        public decimal StepSize { get; set; }

        /// <summary>
        /// Whether trading is allowed
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Minimum order size
        /// </summary>
        [BsonElement("minOrderSize")]
        public decimal MinOrderSize { get; set; }

        /// <summary>
        /// Maximum order size
        /// </summary>
        [BsonElement("maxOrderSize")]
        public decimal MaxOrderSize { get; set; }

        /// <summary>
        /// Fee percentage for takers
        /// </summary>
        [BsonElement("takerFee")]
        public decimal TakerFee { get; set; }

        /// <summary>
        /// Fee percentage for makers
        /// </summary>
        [BsonElement("makerFee")]
        public decimal MakerFee { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Symbol>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Symbol>, bool>>
            {
                new Tuple<IndexKeysDefinition<Symbol>, bool>(
                    Builders<Symbol>.IndexKeys.Ascending(s => s.Name),
                    true
                )
            };
        }
    }
}