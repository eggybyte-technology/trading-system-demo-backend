using System;
using System.Collections.Generic;

namespace CommonLib.Models.MatchMaking
{
    /// <summary>
    /// Response with matching service status
    /// </summary>
    public class MatchingStatusResponse
    {
        /// <summary>
        /// Whether the matching service is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Current status of the matching service
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Number of processed batches since startup
        /// </summary>
        public int ProcessedBatchesCount { get; set; }

        /// <summary>
        /// Timestamp of the last matching run
        /// </summary>
        public long LastRunTimestamp { get; set; }

        /// <summary>
        /// Timestamp of the next scheduled run
        /// </summary>
        public long NextScheduledRunTimestamp { get; set; }

        /// <summary>
        /// Symbols enabled for matching
        /// </summary>
        public string[] EnabledSymbols { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Number of orders in queue for processing
        /// </summary>
        public int QueuedOrdersCount { get; set; }

        /// <summary>
        /// Breakdown of orders per symbol
        /// </summary>
        public Dictionary<string, int> OrdersPerSymbol { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Response for a matching operation result
    /// </summary>
    public class MatchingResultResponse
    {
        /// <summary>
        /// Unique batch identifier
        /// </summary>
        public string BatchId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the matching was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of orders processed
        /// </summary>
        public int ProcessedOrdersCount { get; set; }

        /// <summary>
        /// Number of orders matched
        /// </summary>
        public int MatchedOrdersCount { get; set; }

        /// <summary>
        /// Number of trades created
        /// </summary>
        public int CreatedTradesCount { get; set; }

        /// <summary>
        /// Breakdown of trades per symbol
        /// </summary>
        public Dictionary<string, int> TradesPerSymbol { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Total volume traded
        /// </summary>
        public decimal TotalTradedVolume { get; set; }

        /// <summary>
        /// Breakdown of volume per symbol
        /// </summary>
        public Dictionary<string, decimal> VolumePerSymbol { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Error messages if any
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Response for match history
    /// </summary>
    public class MatchHistoryResponse
    {
        /// <summary>
        /// Current page
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Match batches
        /// </summary>
        public List<MatchBatchInfo> Items { get; set; } = new List<MatchBatchInfo>();
    }

    /// <summary>
    /// Information about a match batch
    /// </summary>
    public class MatchBatchInfo
    {
        /// <summary>
        /// Batch identifier
        /// </summary>
        public string BatchId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when batch was processed
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Number of orders processed
        /// </summary>
        public int ProcessedOrdersCount { get; set; }

        /// <summary>
        /// Number of orders matched
        /// </summary>
        public int MatchedOrdersCount { get; set; }

        /// <summary>
        /// Number of trades created
        /// </summary>
        public int CreatedTradesCount { get; set; }

        /// <summary>
        /// Breakdown of trades per symbol
        /// </summary>
        public Dictionary<string, int> TradesPerSymbol { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Total volume traded
        /// </summary>
        public decimal TotalTradedVolume { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Response with matching settings
    /// </summary>
    public class MatchingSettingsResponse
    {
        /// <summary>
        /// Matching interval in seconds
        /// </summary>
        public int MatchingIntervalSeconds { get; set; }

        /// <summary>
        /// Order lock timeout in seconds
        /// </summary>
        public int OrderLockTimeoutSeconds { get; set; }

        /// <summary>
        /// Symbols enabled for matching
        /// </summary>
        public string[] EnabledSymbols { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Maximum orders to process per batch
        /// </summary>
        public int MaxOrdersPerBatch { get; set; }

        /// <summary>
        /// Maximum trades to create per batch
        /// </summary>
        public int MaxTradesPerBatch { get; set; }

        /// <summary>
        /// Whether matching is enabled
        /// </summary>
        public bool IsMatchingEnabled { get; set; }
    }

    /// <summary>
    /// Response with matching statistics
    /// </summary>
    public class MatchingStatsResponse
    {
        /// <summary>
        /// Total number of batches processed
        /// </summary>
        public int TotalBatchesProcessed { get; set; }

        /// <summary>
        /// Total number of trades created
        /// </summary>
        public int TotalTradesCreated { get; set; }

        /// <summary>
        /// Total volume traded
        /// </summary>
        public decimal TotalVolumeTraded { get; set; }

        /// <summary>
        /// Breakdown of volume per symbol
        /// </summary>
        public Dictionary<string, decimal> VolumePerSymbol { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// Average matching time in milliseconds
        /// </summary>
        public decimal AverageMatchingTimeMs { get; set; }

        /// <summary>
        /// Number of trades in the last 24 hours
        /// </summary>
        public long LastDayTradesCount { get; set; }

        /// <summary>
        /// Volume traded in the last 24 hours
        /// </summary>
        public decimal LastDayVolume { get; set; }

        /// <summary>
        /// Statistics per symbol
        /// </summary>
        public Dictionary<string, SymbolStats> SymbolStatistics { get; set; } = new Dictionary<string, SymbolStats>();
    }

    /// <summary>
    /// Statistics for a symbol
    /// </summary>
    public class SymbolStats
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Number of trades
        /// </summary>
        public int TradesCount { get; set; }

        /// <summary>
        /// Total volume
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Average price
        /// </summary>
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// Highest price
        /// </summary>
        public decimal HighestPrice { get; set; }

        /// <summary>
        /// Lowest price
        /// </summary>
        public decimal LowestPrice { get; set; }
    }

    /// <summary>
    /// Response for a test matching operation
    /// </summary>
    public class TestMatchingResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Matches created
        /// </summary>
        public List<TestMatch> Matches { get; set; } = new List<TestMatch>();

        /// <summary>
        /// Orders that were not matched
        /// </summary>
        public List<TestOrder> UnmatchedOrders { get; set; } = new List<TestOrder>();

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Test match result
    /// </summary>
    public class TestMatch
    {
        /// <summary>
        /// Buy order
        /// </summary>
        public TestOrder BuyOrder { get; set; } = null!;

        /// <summary>
        /// Sell order
        /// </summary>
        public TestOrder SellOrder { get; set; } = null!;

        /// <summary>
        /// Matched quantity
        /// </summary>
        public decimal MatchedQuantity { get; set; }

        /// <summary>
        /// Matched price
        /// </summary>
        public decimal MatchedPrice { get; set; }
    }
}