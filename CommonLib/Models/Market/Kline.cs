using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Candlestick data
    /// </summary>
    public class Kline : IndexedModel<Kline>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "MarketDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Klines";

        /// <summary>
        /// Symbol name
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Interval (1m, 5m, 15m, 30m, 1h, 4h, 1d, 1w, 1M)
        /// </summary>
        [BsonElement("interval")]
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Open time
        /// </summary>
        [BsonElement("openTime")]
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Close time
        /// </summary>
        [BsonElement("closeTime")]
        public DateTime CloseTime { get; set; }

        /// <summary>
        /// Open price
        /// </summary>
        [BsonElement("open")]
        public decimal Open { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        [BsonElement("high")]
        public decimal High { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        [BsonElement("low")]
        public decimal Low { get; set; }

        /// <summary>
        /// Close price
        /// </summary>
        [BsonElement("close")]
        public decimal Close { get; set; }

        /// <summary>
        /// Volume
        /// </summary>
        [BsonElement("volume")]
        public decimal Volume { get; set; }

        /// <summary>
        /// Quote asset volume
        /// </summary>
        [BsonElement("quoteVolume")]
        public decimal QuoteVolume { get; set; }

        /// <summary>
        /// Number of trades
        /// </summary>
        [BsonElement("tradeCount")]
        public int TradeCount { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Kline>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Kline>, bool>>
            {
                new Tuple<IndexKeysDefinition<Kline>, bool>(
                    Builders<Kline>.IndexKeys.Ascending(k => k.Symbol)
                        .Ascending(k => k.Interval)
                        .Ascending(k => k.OpenTime),
                    true
                )
            };
        }
    }
}