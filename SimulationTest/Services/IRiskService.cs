using CommonLib.Models.Risk;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for risk management operations
    /// </summary>
    public interface IRiskService
    {
        /// <summary>
        /// Gets risk status for the current user
        /// </summary>
        /// <returns>Risk profile</returns>
        Task<RiskProfile> GetRiskStatusAsync();

        /// <summary>
        /// Gets trading limits for the current user
        /// </summary>
        /// <returns>List of risk rules</returns>
        Task<List<RiskRule>> GetTradingLimitsAsync();

        /// <summary>
        /// Acknowledges a risk alert
        /// </summary>
        /// <param name="alertId">Alert ID</param>
        /// <returns>Updated risk alert</returns>
        Task<RiskAlert> AcknowledgeAlertAsync(string alertId);
    }
}