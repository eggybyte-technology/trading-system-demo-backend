using System.Collections.Generic;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Represents the result of a matching operation
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Whether the order was fully matched
        /// </summary>
        public bool IsFullyMatched { get; set; }

        /// <summary>
        /// List of trades that resulted from the match
        /// </summary>
        public List<Trade> Trades { get; set; } = new();

        /// <summary>
        /// Remaining quantity to be filled
        /// </summary>
        public decimal RemainingQuantity { get; set; }
    }
}