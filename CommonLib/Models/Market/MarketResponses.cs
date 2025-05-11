using System;
using System.Collections.Generic;

namespace CommonLib.Models.Market
{
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
        /// Base asset precision
        /// </summary>
        public int BaseAssetPrecision { get; set; }

        /// <summary>
        /// Quote precision
        /// </summary>
        public int QuotePrecision { get; set; }

        /// <summary>
        /// Whether the symbol is currently active for trading
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Symbols response
    /// </summary>
    public class SymbolsResponse
    {
        /// <summary>
        /// List of symbol information
        /// </summary>
        public List<SymbolInfo> Symbols { get; set; } = new List<SymbolInfo>();
    }

    /// <summary>
    /// Ticker information
    /// </summary>
    public class TickerResponse
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Current price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Price change
        /// </summary>
        public decimal PriceChange { get; set; }

        /// <summary>
        /// Price change percent
        /// </summary>
        public decimal PriceChangePercent { get; set; }

        /// <summary>
        /// 24h high
        /// </summary>
        public decimal High24h { get; set; }

        /// <summary>
        /// 24h low
        /// </summary>
        public decimal Low24h { get; set; }

        /// <summary>
        /// 24h volume
        /// </summary>
        public decimal Volume24h { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Market summary response
    /// </summary>
    public class MarketSummaryResponse
    {
        /// <summary>
        /// List of market ticker information
        /// </summary>
        public List<TickerResponse> Markets { get; set; } = new List<TickerResponse>();
    }

    /// <summary>
    /// Market depth response
    /// </summary>
    public class MarketDepthResponse
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Bids (price, quantity)
        /// </summary>
        public List<decimal[]> Bids { get; set; } = new List<decimal[]>();

        /// <summary>
        /// Asks (price, quantity)
        /// </summary>
        public List<decimal[]> Asks { get; set; } = new List<decimal[]>();
    }

    /// <summary>
    /// Trade information
    /// </summary>
    public class TradeResponse
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Symbol name
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
        /// Trade timestamp
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// Whether buyer is maker
        /// </summary>
        public bool IsBuyerMaker { get; set; }
    }
}