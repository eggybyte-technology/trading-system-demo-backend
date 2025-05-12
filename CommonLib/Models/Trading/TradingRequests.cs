using System;
using System.Collections.Generic;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Order creation request model
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// Trading pair symbol (e.g., BTC-USDT)
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Order side (BUY, SELL)
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Order type (LIMIT, MARKET, STOP_LOSS, STOP_LOSS_LIMIT)
        /// </summary>
        public string Type { get; set; } = "LIMIT";

        /// <summary>
        /// Time-in-force (GTC, IOC, FOK)
        /// </summary>
        public string TimeInForce { get; set; } = "GTC";

        /// <summary>
        /// Order quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Order price (required for LIMIT orders)
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Stop price (for STOP_LOSS and STOP_LOSS_LIMIT orders)
        /// </summary>
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// Iceberg quantity (for iceberg orders)
        /// </summary>
        public decimal? IcebergQty { get; set; }
    }

    /// <summary>
    /// Order history request parameters
    /// </summary>
    public class OrderHistoryRequest
    {
        /// <summary>
        /// Symbol to filter by
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Status to filter by
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Start time (Unix timestamp in seconds)
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// End time (Unix timestamp in seconds)
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}