using System;
using System.Collections.Generic;

namespace CommonLib.Models.MatchMaking
{
    /// <summary>
    /// Request to trigger the matching engine
    /// </summary>
    public class TriggerMatchingRequest
    {
        /// <summary>
        /// Symbols to match orders for
        /// </summary>
        public string[] Symbols { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Whether to force run the matching engine, ignoring the schedule
        /// </summary>
        public bool ForceRun { get; set; } = false;
    }

    /// <summary>
    /// Request for match history
    /// </summary>
    public class MatchHistoryRequest
    {
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Start time in milliseconds
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// End time in milliseconds
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Symbol to filter by
        /// </summary>
        public string? Symbol { get; set; }
    }

    /// <summary>
    /// Request to update matching settings
    /// </summary>
    public class UpdateMatchingSettingsRequest
    {
        /// <summary>
        /// Matching interval in seconds
        /// </summary>
        public int? MatchingIntervalSeconds { get; set; }

        /// <summary>
        /// Order lock timeout in seconds
        /// </summary>
        public int? OrderLockTimeoutSeconds { get; set; }

        /// <summary>
        /// Symbols enabled for matching
        /// </summary>
        public string[]? EnabledSymbols { get; set; }

        /// <summary>
        /// Maximum orders to process per batch
        /// </summary>
        public int? MaxOrdersPerBatch { get; set; }

        /// <summary>
        /// Maximum trades to create per batch
        /// </summary>
        public int? MaxTradesPerBatch { get; set; }

        /// <summary>
        /// Whether matching is enabled
        /// </summary>
        public bool? IsMatchingEnabled { get; set; }
    }

    /// <summary>
    /// Request to test matching algorithm with sample orders
    /// </summary>
    public class TestMatchingRequest
    {
        /// <summary>
        /// Symbol to test
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Test orders
        /// </summary>
        public List<TestOrder> Orders { get; set; } = new List<TestOrder>();
    }

    /// <summary>
    /// Test order for simulation
    /// </summary>
    public class TestOrder
    {
        /// <summary>
        /// Order side (BUY or SELL)
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Order price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Order quantity
        /// </summary>
        public decimal Quantity { get; set; }
    }
}