using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Risk;
using MongoDB.Bson;

namespace RiskService.Services
{
    /// <summary>
    /// Interface for risk management services
    /// </summary>
    public interface IRiskService
    {
        /// <summary>
        /// Get risk profile for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Risk profile or null if not found</returns>
        Task<RiskProfile?> GetRiskProfileAsync(ObjectId userId);

        /// <summary>
        /// Get trading limits for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Trading limits or null if not found</returns>
        Task<TradingLimits?> GetTradingLimitsAsync(ObjectId userId);

        /// <summary>
        /// Get active alerts for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of active risk alerts</returns>
        Task<List<RiskAlert>> GetActiveAlertsAsync(ObjectId userId);

        /// <summary>
        /// Acknowledge a risk alert
        /// </summary>
        /// <param name="alertId">Alert ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="comment">Optional acknowledgment comment</param>
        /// <returns>Updated risk alert or null if not found</returns>
        Task<RiskAlert?> AcknowledgeAlertAsync(ObjectId alertId, ObjectId userId, string? comment = null);

        /// <summary>
        /// Get active risk rules
        /// </summary>
        /// <returns>List of active risk rules</returns>
        Task<List<RiskRule>> GetActiveRulesAsync();
    }
}