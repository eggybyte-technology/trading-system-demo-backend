using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommonLib.Models.MatchMaking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    /// <summary>
    /// API client for the MatchMaking service
    /// </summary>
    public class MatchMakingService : BaseService
    {
        public MatchMakingService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "MatchMakingService", "http://match-making.trading-system.local", logger)
        {
        }

        /// <summary>
        /// Get the current status of the matching service
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns>The matching service status</returns>
        public async Task<MatchingStatusResponse> GetStatusAsync(string token)
        {
            return await GetAsync<MatchingStatusResponse>("/matchmaking/status", token);
        }

        /// <summary>
        /// Trigger the matching engine to run
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Matching trigger request</param>
        /// <returns>The result of the matching operation</returns>
        public async Task<MatchingResultResponse> TriggerMatchingAsync(string token, TriggerMatchingRequest request)
        {
            return await PostAsync<MatchingResultResponse, TriggerMatchingRequest>("/matchmaking/trigger", request, token);
        }

        /// <summary>
        /// Get matching history with pagination
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">History query parameters</param>
        /// <returns>Paginated match history</returns>
        public async Task<MatchHistoryResponse> GetMatchHistoryAsync(string token, MatchHistoryRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = request.Page.ToString(),
                ["pageSize"] = request.PageSize.ToString(),
                ["symbol"] = request.Symbol,
                ["startTime"] = request.StartTime?.ToString(),
                ["endTime"] = request.EndTime?.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<MatchHistoryResponse>($"/matchmaking/history?{queryString}", token);
        }

        /// <summary>
        /// Get the current matching settings
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns>Current matching settings</returns>
        public async Task<MatchingSettingsResponse> GetSettingsAsync(string token)
        {
            return await GetAsync<MatchingSettingsResponse>("/matchmaking/settings", token);
        }

        /// <summary>
        /// Update matching settings
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Settings update request</param>
        /// <returns>Updated matching settings</returns>
        public async Task<MatchingSettingsResponse> UpdateSettingsAsync(string token, UpdateMatchingSettingsRequest request)
        {
            return await PutAsync<MatchingSettingsResponse, UpdateMatchingSettingsRequest>("/matchmaking/settings", request, token);
        }

        /// <summary>
        /// Get matching statistics
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns>Current matching statistics</returns>
        public async Task<MatchingStatsResponse> GetStatsAsync(string token)
        {
            return await GetAsync<MatchingStatsResponse>("/matchmaking/stats", token);
        }

        /// <summary>
        /// Test the matching algorithm with sample orders
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Test matching request with orders</param>
        /// <returns>Test matching results</returns>
        public async Task<TestMatchingResponse> TestMatchingAsync(string token, TestMatchingRequest request)
        {
            return await PostAsync<TestMatchingResponse, TestMatchingRequest>("/matchmaking/test", request, token);
        }
    }
}