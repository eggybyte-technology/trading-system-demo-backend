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
}