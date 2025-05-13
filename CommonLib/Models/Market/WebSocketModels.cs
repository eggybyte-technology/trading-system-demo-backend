using System;
using System.Collections.Generic;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Base WebSocket message model
    /// </summary>
    public class WebSocketMessage
    {
        /// <summary>
        /// Message type (e.g., ticker, depth, trade, kline, userData)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The trading pair symbol
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Message timestamp
        /// </summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Message data
        /// </summary>
        public object? Data { get; set; }
    }

    /// <summary>
    /// Subscription request model
    /// </summary>
    public class SubscriptionRequest
    {
        /// <summary>
        /// Request type (SUBSCRIBE, UNSUBSCRIBE)
        /// </summary>
        public string Type { get; set; } = "SUBSCRIBE";

        /// <summary>
        /// Channels to subscribe to (e.g., ticker, depth, trade, kline, userData)
        /// </summary>
        public List<string> Channels { get; set; } = new();

        /// <summary>
        /// Trading pair symbols to subscribe to
        /// </summary>
        public List<string> Symbols { get; set; } = new();
    }

    /// <summary>
    /// Ticker data for WebSocket updates
    /// </summary>
    public class WebSocketTickerData
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Price change
        /// </summary>
        public decimal PriceChange { get; set; }

        /// <summary>
        /// Price change percent
        /// </summary>
        public decimal PriceChangePercent { get; set; }

        /// <summary>
        /// Last price
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// 24h volume
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 24h high price
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// 24h low price
        /// </summary>
        public decimal LowPrice { get; set; }
    }

    /// <summary>
    /// Order book depth data for WebSocket updates
    /// </summary>
    public class WebSocketDepthData
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Bid levels [price, quantity]
        /// </summary>
        public List<List<decimal>> Bids { get; set; } = new();

        /// <summary>
        /// Ask levels [price, quantity]
        /// </summary>
        public List<List<decimal>> Asks { get; set; } = new();
    }

    /// <summary>
    /// Trade data for WebSocket updates
    /// </summary>
    public class WebSocketTradeData
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
        /// Whether the buyer is the market maker
        /// </summary>
        public bool IsBuyerMaker { get; set; }

        /// <summary>
        /// Trade time (Unix timestamp in milliseconds)
        /// </summary>
        public long Time { get; set; }
    }

    /// <summary>
    /// Kline (candlestick) data for WebSocket updates
    /// </summary>
    public class KlineData
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Interval (e.g., 1m, 5m, 15m, 1h, 4h, 1d, 1w)
        /// </summary>
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Open time (Unix timestamp in milliseconds)
        /// </summary>
        public long OpenTime { get; set; }

        /// <summary>
        /// Close time (Unix timestamp in milliseconds)
        /// </summary>
        public long CloseTime { get; set; }

        /// <summary>
        /// Open price
        /// </summary>
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// Close price
        /// </summary>
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Number of trades
        /// </summary>
        public int TradeCount { get; set; }
    }

    /// <summary>
    /// User data update for WebSocket
    /// </summary>
    public class WebSocketUserDataEvent
    {
        /// <summary>
        /// Event type (ORDER_UPDATE, BALANCE_UPDATE)
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Event time (Unix timestamp in milliseconds)
        /// </summary>
        public long EventTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Event data (order or balance update)
        /// </summary>
        public object? Data { get; set; }
    }

    /// <summary>
    /// Order update data for WebSocket user data channel
    /// </summary>
    public class WebSocketOrderUpdate
    {
        /// <summary>
        /// Order ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Client order ID
        /// </summary>
        public string ClientOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Side (BUY or SELL)
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Order type (LIMIT, MARKET, etc.)
        /// </summary>
        public string OrderType { get; set; } = string.Empty;

        /// <summary>
        /// Time in force (GTC, IOC, FOK)
        /// </summary>
        public string TimeInForce { get; set; } = string.Empty;

        /// <summary>
        /// Original quantity
        /// </summary>
        public decimal OriginalQuantity { get; set; }

        /// <summary>
        /// Executed quantity
        /// </summary>
        public decimal ExecutedQuantity { get; set; }

        /// <summary>
        /// Cumulative quote quantity
        /// </summary>
        public decimal CumulativeQuoteQuantity { get; set; }

        /// <summary>
        /// Order status (NEW, PARTIALLY_FILLED, FILLED, CANCELED, REJECTED, EXPIRED)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Order price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Stop price (for stop orders)
        /// </summary>
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// Iceberg quantity
        /// </summary>
        public decimal? IcebergQuantity { get; set; }

        /// <summary>
        /// Order update time (Unix timestamp in milliseconds)
        /// </summary>
        public long UpdateTime { get; set; }
    }

    /// <summary>
    /// Balance update data for WebSocket user data channel
    /// </summary>
    public class WebSocketBalanceUpdate
    {
        /// <summary>
        /// Asset
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Free balance
        /// </summary>
        public decimal Free { get; set; }

        /// <summary>
        /// Locked balance
        /// </summary>
        public decimal Locked { get; set; }

        /// <summary>
        /// Total balance (free + locked)
        /// </summary>
        public decimal Total => Free + Locked;

        /// <summary>
        /// Update time (Unix timestamp in milliseconds)
        /// </summary>
        public long UpdateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Order book update request model for MarketDataService
    /// </summary>
    public class OrderBookUpdateRequest
    {
        /// <summary>
        /// Symbol for the order book to update
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Bid price levels to add or update [price, quantity]
        /// </summary>
        public List<List<decimal>> Bids { get; set; } = new();

        /// <summary>
        /// Ask price levels to add or update [price, quantity]
        /// </summary>
        public List<List<decimal>> Asks { get; set; } = new();
    }

    /// <summary>
    /// Order book update response
    /// </summary>
    public class OrderBookUpdateResponse
    {
        /// <summary>
        /// Whether the update was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}