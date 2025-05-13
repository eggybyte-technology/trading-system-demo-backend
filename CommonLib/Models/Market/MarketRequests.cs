using System;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Market depth request parameters
    /// </summary>
    public class MarketDepthRequest
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of price levels to return (default: 100, max: 500)
        /// </summary>
        public int Limit { get; set; } = 100;
    }

    /// <summary>
    /// Kline request parameters
    /// </summary>
    public class KlineRequest
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Kline interval (e.g., 1m, 5m, 1h, 1d)
        /// </summary>
        public string Interval { get; set; } = "1h";

        /// <summary>
        /// Optional start time in milliseconds
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// Optional end time in milliseconds
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Maximum number of klines to return (default: 500, max: 1000)
        /// </summary>
        public int Limit { get; set; } = 500;
    }

    /// <summary>
    /// Ticker request parameters
    /// </summary>
    public class TickerRequest
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;
    }

    /// <summary>
    /// Recent trades request parameters
    /// </summary>
    public class RecentTradesRequest
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of trades to return (default: 100, max: 1000)
        /// </summary>
        public int Limit { get; set; } = 100;
    }

    /// <summary>
    /// Request model for processing trade data for kline generation
    /// </summary>
    public class TradeForKlineRequest
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public string TradeId { get; set; } = string.Empty;

        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Trade price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Trade quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Whether the buyer is the market maker
        /// </summary>
        public bool IsBuyerMaker { get; set; }

        /// <summary>
        /// Trade timestamp (Unix timestamp in milliseconds)
        /// </summary>
        public long Timestamp { get; set; }
    }
}