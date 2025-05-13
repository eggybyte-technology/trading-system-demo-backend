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

    /// <summary>
    /// Trade history request parameters
    /// </summary>
    public class TradeHistoryRequest
    {
        /// <summary>
        /// Symbol to filter by
        /// </summary>
        public string? Symbol { get; set; }

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

    /// <summary>
    /// Open orders request parameters
    /// </summary>
    public class OpenOrdersRequest
    {
        /// <summary>
        /// Symbol to filter by (optional)
        /// </summary>
        public string? Symbol { get; set; }
    }

    /// <summary>
    /// Request to lock an order for processing
    /// </summary>
    public class LockOrderRequest
    {
        /// <summary>
        /// Order ID to lock
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Unique lock identifier
        /// </summary>
        public string LockId { get; set; }

        /// <summary>
        /// Timeout for lock in seconds (default: 5 seconds)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 5;
    }

    /// <summary>
    /// Request to unlock a previously locked order
    /// </summary>
    public class UnlockOrderRequest
    {
        /// <summary>
        /// Order ID to unlock
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Lock ID that was used to lock the order
        /// </summary>
        public string LockId { get; set; }
    }

    /// <summary>
    /// Request to update order status after processing
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        /// <summary>
        /// Order ID to update
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// New order status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Updated executed quantity
        /// </summary>
        public decimal ExecutedQuantity { get; set; }

        /// <summary>
        /// Updated cumulative quote quantity
        /// </summary>
        public decimal CumulativeQuoteQuantity { get; set; }

        /// <summary>
        /// Lock ID that was used to lock the order (optional)
        /// </summary>
        public string? LockId { get; set; }
    }
}