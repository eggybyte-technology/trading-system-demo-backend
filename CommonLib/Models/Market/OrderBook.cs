using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Order book for a symbol
    /// </summary>
    public class OrderBook : IndexedModel<OrderBook>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "MarketDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "OrderBooks";

        /// <summary>
        /// Symbol name
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// List of bids (price, quantity)
        /// </summary>
        [BsonElement("bids")]
        public List<PriceLevel> Bids { get; set; } = new();

        /// <summary>
        /// List of asks (price, quantity)
        /// </summary>
        [BsonElement("asks")]
        public List<PriceLevel> Asks { get; set; } = new();

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<OrderBook>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<OrderBook>, bool>>
            {
                new Tuple<IndexKeysDefinition<OrderBook>, bool>(
                    Builders<OrderBook>.IndexKeys.Ascending(o => o.Symbol),
                    true
                )
            };
        }
    }
}