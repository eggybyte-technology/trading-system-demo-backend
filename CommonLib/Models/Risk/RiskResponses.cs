using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// Risk profile response model
    /// </summary>
    public class RiskProfileResponse
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Risk level (LOW, MEDIUM, HIGH)
        /// </summary>
        public string RiskLevel { get; set; } = string.Empty;

        /// <summary>
        /// Risk score (0-100)
        /// </summary>
        public int RiskScore { get; set; }

        /// <summary>
        /// Total position value
        /// </summary>
        public decimal TotalPositionValue { get; set; }

        /// <summary>
        /// Maximum allowed position value
        /// </summary>
        public decimal MaxPositionValue { get; set; }

        /// <summary>
        /// Daily trading volume
        /// </summary>
        public decimal DailyTradingVolume { get; set; }

        /// <summary>
        /// List of active alerts
        /// </summary>
        public List<RiskAlertResponse> ActiveAlerts { get; set; } = new List<RiskAlertResponse>();

        /// <summary>
        /// Last update timestamp in milliseconds
        /// </summary>
        public long UpdatedAt { get; set; }
    }

    /// <summary>
    /// Risk alert response model
    /// </summary>
    public class RiskAlertResponse
    {
        /// <summary>
        /// Alert ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Alert type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Alert severity (WARNING, CRITICAL)
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Alert message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Whether the alert has been acknowledged
        /// </summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// Acknowledgment timestamp in milliseconds (if acknowledged)
        /// </summary>
        public long? AcknowledgedAt { get; set; }

        /// <summary>
        /// Acknowledgment comment
        /// </summary>
        public string? AcknowledgmentComment { get; set; }

        /// <summary>
        /// Creation timestamp in milliseconds
        /// </summary>
        public long CreatedAt { get; set; }
    }

    /// <summary>
    /// Risk rule response model
    /// </summary>
    public class RiskRuleResponse
    {
        /// <summary>
        /// Rule ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

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
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Last update timestamp in milliseconds
        /// </summary>
        public long UpdatedAt { get; set; }
    }
}