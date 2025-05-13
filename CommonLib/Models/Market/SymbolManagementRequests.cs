using System;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Request model for creating a new trading symbol
    /// </summary>
    public class SymbolCreateRequest
    {
        /// <summary>
        /// Symbol name (e.g., "BTC-USDT")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Base asset (e.g., "BTC")
        /// </summary>
        public string BaseAsset { get; set; } = string.Empty;

        /// <summary>
        /// Quote asset (e.g., "USDT")
        /// </summary>
        public string QuoteAsset { get; set; } = string.Empty;

        /// <summary>
        /// Base asset precision
        /// </summary>
        public int BaseAssetPrecision { get; set; } = 8;

        /// <summary>
        /// Quote asset precision
        /// </summary>
        public int QuotePrecision { get; set; } = 2;

        /// <summary>
        /// Minimum price
        /// </summary>
        public decimal MinPrice { get; set; }

        /// <summary>
        /// Maximum price
        /// </summary>
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// Tick size - minimum price movement
        /// </summary>
        public decimal TickSize { get; set; }

        /// <summary>
        /// Minimum quantity
        /// </summary>
        public decimal MinQty { get; set; }

        /// <summary>
        /// Maximum quantity
        /// </summary>
        public decimal MaxQty { get; set; }

        /// <summary>
        /// Step size - minimum quantity movement
        /// </summary>
        public decimal StepSize { get; set; }

        /// <summary>
        /// Whether trading is allowed
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Minimum order size in quote currency
        /// </summary>
        public decimal MinOrderSize { get; set; }

        /// <summary>
        /// Maximum order size in quote currency
        /// </summary>
        public decimal MaxOrderSize { get; set; }

        /// <summary>
        /// Fee percentage for takers
        /// </summary>
        public decimal TakerFee { get; set; } = 0.001m;

        /// <summary>
        /// Fee percentage for makers
        /// </summary>
        public decimal MakerFee { get; set; } = 0.001m;
    }

    /// <summary>
    /// Request model for updating an existing trading symbol
    /// </summary>
    public class SymbolUpdateRequest
    {
        /// <summary>
        /// Minimum price
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Tick size - minimum price movement
        /// </summary>
        public decimal? TickSize { get; set; }

        /// <summary>
        /// Minimum quantity
        /// </summary>
        public decimal? MinQty { get; set; }

        /// <summary>
        /// Maximum quantity
        /// </summary>
        public decimal? MaxQty { get; set; }

        /// <summary>
        /// Step size - minimum quantity movement
        /// </summary>
        public decimal? StepSize { get; set; }

        /// <summary>
        /// Whether trading is allowed
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Minimum order size in quote currency
        /// </summary>
        public decimal? MinOrderSize { get; set; }

        /// <summary>
        /// Maximum order size in quote currency
        /// </summary>
        public decimal? MaxOrderSize { get; set; }

        /// <summary>
        /// Fee percentage for takers
        /// </summary>
        public decimal? TakerFee { get; set; }

        /// <summary>
        /// Fee percentage for makers
        /// </summary>
        public decimal? MakerFee { get; set; }
    }

    /// <summary>
    /// Response model for symbol operations
    /// </summary>
    public class SymbolResponse
    {
        /// <summary>
        /// Success flag
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Symbol information if operation was successful
        /// </summary>
        public SymbolInfo? Data { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? Message { get; set; }
    }
}