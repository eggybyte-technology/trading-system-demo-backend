using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Match engine status response
    /// </summary>
    public class MatchStatusResponse
    {
        /// <summary>
        /// Whether the match engine is running
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Current processing rate (orders per second)
        /// </summary>
        public decimal ProcessingRate { get; set; }

        /// <summary>
        /// Number of pending orders
        /// </summary>
        public int PendingOrders { get; set; }

        /// <summary>
        /// Number of active symbols being processed
        /// </summary>
        public int ActiveSymbols { get; set; }

        /// <summary>
        /// Last update timestamp in milliseconds
        /// </summary>
        public long UpdatedAt { get; set; }
    }

    /// <summary>
    /// Matching job response
    /// </summary>
    public class MatchingJobResponse
    {
        /// <summary>
        /// Job ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Number of orders processed
        /// </summary>
        public int OrdersProcessed { get; set; }

        /// <summary>
        /// Number of trades generated
        /// </summary>
        public int TradesGenerated { get; set; }

        /// <summary>
        /// Job status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Error message (if any)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Start time in milliseconds
        /// </summary>
        public long StartTime { get; set; }

        /// <summary>
        /// End time in milliseconds
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Processing duration in milliseconds
        /// </summary>
        public long? ProcessingDuration { get; set; }
    }
}