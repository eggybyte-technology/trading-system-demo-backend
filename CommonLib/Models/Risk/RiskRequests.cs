using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// Request to acknowledge a risk alert
    /// </summary>
    public class AcknowledgeAlertRequest
    {
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
        /// Rule name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rule description
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Maximum position size
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? MaxPositionSize { get; set; }

        /// <summary>
        /// Maximum order size
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? MaxOrderSize { get; set; }

        /// <summary>
        /// Maximum orders per day
        /// </summary>
        [Range(0, int.MaxValue)]
        public int? MaxOrdersPerDay { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Request to create a new risk rule
    /// </summary>
    public class CreateRiskRuleRequest
    {
        /// <summary>
        /// Rule name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rule description
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Rule type (transaction, order, user)
        /// </summary>
        [Required]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Maximum position size
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? MaxPositionSize { get; set; }

        /// <summary>
        /// Maximum order size
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? MaxOrderSize { get; set; }

        /// <summary>
        /// Maximum orders per day
        /// </summary>
        [Range(0, int.MaxValue)]
        public int? MaxOrdersPerDay { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Additional parameters in JSON format
        /// </summary>
        public string Parameters { get; set; } = "{}";
    }

    /// <summary>
    /// Request to get risk alerts with filtering parameters
    /// </summary>
    public class GetRiskAlertsRequest
    {
        /// <summary>
        /// Filter by alert type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filter by severity
        /// </summary>
        public string? Severity { get; set; }

        /// <summary>
        /// Filter by acknowledgment status
        /// </summary>
        public bool? IsAcknowledged { get; set; }

        /// <summary>
        /// Start time for alerts (Unix timestamp in milliseconds)
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// End time for alerts (Unix timestamp in milliseconds)
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}