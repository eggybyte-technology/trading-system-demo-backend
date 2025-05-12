using System;
using MongoDB.Bson;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Request to retrieve matching job details
    /// </summary>
    public class GetMatchingJobsRequest
    {
        /// <summary>
        /// Symbol to filter by
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Start time in milliseconds
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// End time in milliseconds
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Status to filter by
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}