using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Market data for a symbol
    /// </summary>
    public class MarketData : IndexedModel<MarketData>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "MarketDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "MarketData";

        /// <summary>
        /// Symbol name
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Last price
        /// </summary>
        [BsonElement("lastPrice")]
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Price change in 24h
        /// </summary>
        [BsonElement("priceChange")]
        public decimal PriceChange { get; set; }

        /// <summary>
        /// Price change percentage in 24h
        /// </summary>
        [BsonElement("priceChangePercent")]
        public decimal PriceChangePercent { get; set; }

        /// <summary>
        /// Highest price in 24h
        /// </summary>
        [BsonElement("high24h")]
        public decimal High24h { get; set; }

        /// <summary>
        /// Lowest price in 24h
        /// </summary>
        [BsonElement("low24h")]
        public decimal Low24h { get; set; }

        /// <summary>
        /// Trading volume in 24h
        /// </summary>
        [BsonElement("volume24h")]
        public decimal Volume24h { get; set; }

        /// <summary>
        /// Quote asset volume in 24h
        /// </summary>
        [BsonElement("quoteVolume24h")]
        public decimal QuoteVolume24h { get; set; }

        /// <summary>
        /// Last updated time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<MarketData>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<MarketData>, bool>>
            {
                new Tuple<IndexKeysDefinition<MarketData>, bool>(
                    Builders<MarketData>.IndexKeys.Ascending(m => m.Symbol),
                    true
                )
            };
        }
    }
}