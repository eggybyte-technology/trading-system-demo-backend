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
    /// Order history response
    /// </summary>
    public class OrderHistoryResponse
    {
        /// <summary>
        /// Total number of orders
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Current page
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Orders on current page
        /// </summary>
        public List<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
    }

    /// <summary>
    /// Response model for order details
    /// </summary>
    public class OrderResponse
    {
        /// <summary>
        /// Order ID
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Original quantity
        /// </summary>
        public decimal OrigQty { get; set; }

        /// <summary>
        /// Executed quantity
        /// </summary>
        public decimal ExecutedQty { get; set; }

        /// <summary>
        /// Order status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Time in force
        /// </summary>
        public string TimeInForce { get; set; } = string.Empty;

        /// <summary>
        /// Order type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Order side
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Stop price
        /// </summary>
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// Iceberg quantity
        /// </summary>
        public decimal? IcebergQty { get; set; }

        /// <summary>
        /// Creation time (Unix timestamp)
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// Update time (Unix timestamp)
        /// </summary>
        public long UpdateTime { get; set; }

        /// <summary>
        /// Whether the order is working
        /// </summary>
        public bool IsWorking { get; set; }

        /// <summary>
        /// Trade fills for the order
        /// </summary>
        public List<OrderFill> Fills { get; set; } = new List<OrderFill>();
    }

    /// <summary>
    /// Order fill details
    /// </summary>
    public class OrderFill
    {
        /// <summary>
        /// Price for this fill
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity for this fill
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Commission charged
        /// </summary>
        public decimal Commission { get; set; }

        /// <summary>
        /// Commission asset
        /// </summary>
        public string CommissionAsset { get; set; } = string.Empty;

        /// <summary>
        /// Trade ID
        /// </summary>
        public string TradeId { get; set; } = string.Empty;

        /// <summary>
        /// Trade timestamp
        /// </summary>
        public long Time { get; set; }
    }

    /// <summary>
    /// Trade response model
    /// </summary>
    public class TradeResponse
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Trade timestamp
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// Whether the buyer was the maker
        /// </summary>
        public bool IsBuyerMaker { get; set; }
    }
}