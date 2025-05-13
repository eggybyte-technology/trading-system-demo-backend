using System;
using System.Collections.Generic;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Response model containing all symbols
    /// </summary>
    public class SymbolsResponse
    {
        /// <summary>
        /// List of available symbols
        /// </summary>
        public List<SymbolInfo> Symbols { get; set; } = new List<SymbolInfo>();
    }

    /// <summary>
    /// Symbol information
    /// </summary>
    public class SymbolInfo
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Base asset
        /// </summary>
        public string BaseAsset { get; set; } = string.Empty;

        /// <summary>
        /// Quote asset
        /// </summary>
        public string QuoteAsset { get; set; } = string.Empty;

        /// <summary>
        /// Minimum order size
        /// </summary>
        public decimal MinOrderSize { get; set; }

        /// <summary>
        /// Maximum order size
        /// </summary>
        public decimal MaxOrderSize { get; set; }

        /// <summary>
        /// Price precision
        /// </summary>
        public int PricePrecision { get; set; }

        /// <summary>
        /// Quantity precision
        /// </summary>
        public int QuantityPrecision { get; set; }

        /// <summary>
        /// Is active
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Response model for ticker data
    /// </summary>
    public class TickerResponse
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Last price
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Price change
        /// </summary>
        public decimal PriceChange { get; set; }

        /// <summary>
        /// Price change percentage
        /// </summary>
        public decimal PriceChangePercent { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Quote volume
        /// </summary>
        public decimal QuoteVolume { get; set; }

        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for market summary
    /// </summary>
    public class MarketSummaryResponse
    {
        /// <summary>
        /// List of tickers
        /// </summary>
        public List<TickerResponse> Tickers { get; set; } = new List<TickerResponse>();

        /// <summary>
        /// Total trading volume
        /// </summary>
        public decimal TotalVolume { get; set; }

        /// <summary>
        /// Total trading quote volume
        /// </summary>
        public decimal TotalQuoteVolume { get; set; }

        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for market depth
    /// </summary>
    public class MarketDepthResponse
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Last update ID
        /// </summary>
        public long LastUpdateId { get; set; }

        /// <summary>
        /// Bids (price, quantity)
        /// </summary>
        public List<decimal[]> Bids { get; set; } = new List<decimal[]>();

        /// <summary>
        /// Asks (price, quantity)
        /// </summary>
        public List<decimal[]> Asks { get; set; } = new List<decimal[]>();

        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for a recent trade
    /// </summary>
    public class TradeResponse
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Quote quantity (price * quantity)
        /// </summary>
        public decimal QuoteQuantity { get; set; }

        /// <summary>
        /// Trade time in milliseconds
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// Is buyer maker
        /// </summary>
        public bool IsBuyerMaker { get; set; }

        /// <summary>
        /// Is best match
        /// </summary>
        public bool IsBestMatch { get; set; }
    }

    /// <summary>
    /// Response model for kline/candlestick data
    /// </summary>
    public class KlineResponse
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Interval
        /// </summary>
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Kline data
        /// [0] Open time
        /// [1] Open price
        /// [2] High price
        /// [3] Low price
        /// [4] Close price
        /// [5] Volume
        /// [6] Close time
        /// [7] Quote asset volume
        /// [8] Number of trades
        /// [9] Taker buy base asset volume
        /// [10] Taker buy quote asset volume
        /// </summary>
        public List<decimal[]> Klines { get; set; } = new List<decimal[]>();
    }

    /// <summary>
    /// Response model for multiple trade entries
    /// </summary>
    public class TradesResponse
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// List of trades
        /// </summary>
        public List<TradeResponse> Trades { get; set; } = new List<TradeResponse>();
    }
}