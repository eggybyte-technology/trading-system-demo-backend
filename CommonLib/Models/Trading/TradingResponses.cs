using System;
using System.Collections.Generic;

namespace CommonLib.Models.Trading
{
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
    /// Trade history response
    /// </summary>
    public class TradeHistoryResponse
    {
        /// <summary>
        /// Total number of trades
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Current page
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Trades on current page
        /// </summary>
        public List<TradeResponse> Items { get; set; } = new List<TradeResponse>();
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
        /// Fill price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Fill quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Commission amount
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
        /// Fill time (Unix timestamp)
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
        /// Trade price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Trade quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Trade time (Unix timestamp)
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// Whether buyer is maker
        /// </summary>
        public bool IsBuyerMaker { get; set; }
    }

    /// <summary>
    /// Response model for open orders
    /// </summary>
    public class OpenOrdersResponse
    {
        /// <summary>
        /// List of open orders
        /// </summary>
        public List<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
    }

    /// <summary>
    /// Response model for order cancellation
    /// </summary>
    public class CancelOrderResponse
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
        /// Message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Success indicator
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Response from an order lock operation
    /// </summary>
    public class LockOrderResponse
    {
        /// <summary>
        /// Indicates if the lock was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique lock identifier
        /// </summary>
        public string LockId { get; set; } = string.Empty;

        /// <summary>
        /// Order details
        /// </summary>
        public OrderResponse Order { get; set; }

        /// <summary>
        /// Timestamp when the lock expires (in milliseconds)
        /// </summary>
        public long ExpirationTimestamp { get; set; }

        /// <summary>
        /// Error message if the lock failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Response from an order unlock operation
    /// </summary>
    public class UnlockOrderResponse
    {
        /// <summary>
        /// Indicates if the unlock was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the unlock failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}