using System;
using MongoDB.Bson;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// Request to acknowledge a risk alert
    /// </summary>
    public class AcknowledgeAlertRequest
    {
        /// <summary>
        /// ID of the alert to acknowledge
        /// </summary>
        public string AlertId { get; set; } = string.Empty;

        /// <summary>
        /// Optional comment for the acknowledgment
        /// </summary>
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Request to update a risk rule
    /// </summary>
    public class UpdateRiskRuleRequest
    {
        /// <summary>
        /// Rule ID
        /// </summary>
        public string RuleId { get; set; } = string.Empty;

        /// <summary>
        /// Rule name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rule description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Maximum position size
        /// </summary>
        public decimal? MaxPositionSize { get; set; }

        /// <summary>
        /// Maximum order size
        /// </summary>
        public decimal? MaxOrderSize { get; set; }

        /// <summary>
        /// Maximum orders per day
        /// </summary>
        public int? MaxOrdersPerDay { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}