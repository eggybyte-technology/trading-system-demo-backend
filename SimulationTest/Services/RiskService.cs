using CommonLib.Models.Risk;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Implementation of the risk service for risk management operations
    /// </summary>
    public class RiskService : IRiskService
    {
        private readonly IHttpClientService _httpClient;

        /// <summary>
        /// Initializes a new instance of the RiskService
        /// </summary>
        /// <param name="httpClient">HTTP client service</param>
        public RiskService(IHttpClientService httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets risk status for the current user
        /// </summary>
        /// <returns>Risk profile</returns>
        public async Task<RiskProfile> GetRiskStatusAsync()
        {
            return await _httpClient.GetAsync<RiskProfile>("risk", "risk/status");
        }

        /// <summary>
        /// Gets trading limits for the current user
        /// </summary>
        /// <returns>List of risk rules</returns>
        public async Task<List<RiskRule>> GetTradingLimitsAsync()
        {
            return await _httpClient.GetAsync<List<RiskRule>>("risk", "risk/limits");
        }

        /// <summary>
        /// Acknowledges a risk alert
        /// </summary>
        /// <param name="alertId">Alert ID</param>
        /// <returns>Updated risk alert</returns>
        public async Task<RiskAlert> AcknowledgeAlertAsync(string alertId)
        {
            return await _httpClient.PostAsync<object, RiskAlert>("risk", $"risk/alerts/{alertId}/acknowledge", null);
        }
    }
}