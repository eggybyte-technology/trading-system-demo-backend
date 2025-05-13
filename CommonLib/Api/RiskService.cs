using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommonLib.Models.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    public class RiskService : BaseService
    {
        public RiskService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "RiskService", "http://risk.trading-system.local", logger)
        {
        }

        public async Task<RiskProfileResponse> GetRiskStatusAsync(string token)
        {
            return await GetAsync<RiskProfileResponse>("/risk/status", token);
        }

        public async Task<TradingLimitsResponse> GetTradingLimitsAsync(string token)
        {
            return await GetAsync<TradingLimitsResponse>("/risk/limits", token);
        }

        public async Task<List<RiskAlertResponse>> GetRiskAlertsAsync(string token)
        {
            return await GetAsync<List<RiskAlertResponse>>("/risk/alerts", token);
        }

        public async Task<RiskAlertResponse> AcknowledgeRiskAlertAsync(string token, string alertId, AcknowledgeAlertRequest request = null)
        {
            return await PostAsync<RiskAlertResponse, AcknowledgeAlertRequest>($"/risk/alerts/{alertId}/acknowledge", request, token);
        }

        public async Task<List<RiskRuleResponse>> GetRiskRulesAsync(string token)
        {
            return await GetAsync<List<RiskRuleResponse>>("/risk/rules", token);
        }
    }
}