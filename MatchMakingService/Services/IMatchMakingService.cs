using System;
using System.Threading.Tasks;
using CommonLib.Models.MatchMaking;

namespace MatchMakingService.Services
{
    /// <summary>
    /// Interface for the matching engine service
    /// </summary>
    public interface IMatchMakingService
    {
        /// <summary>
        /// Get the current status of the matching engine
        /// </summary>
        Task<MatchingStatusResponse> GetStatusAsync();

        /// <summary>
        /// Manually trigger a matching cycle
        /// </summary>
        Task<MatchingResultResponse> TriggerMatchingAsync(TriggerMatchingRequest request);

        /// <summary>
        /// Get matching history with pagination
        /// </summary>
        Task<MatchHistoryResponse> GetHistoryAsync(MatchHistoryRequest request);

        /// <summary>
        /// Get current matching engine settings
        /// </summary>
        Task<MatchingSettingsResponse> GetSettingsAsync();

        /// <summary>
        /// Update matching engine settings
        /// </summary>
        Task<MatchingSettingsResponse> UpdateSettingsAsync(UpdateMatchingSettingsRequest request);

        /// <summary>
        /// Get matching statistics
        /// </summary>
        Task<MatchingStatsResponse> GetStatsAsync();

        /// <summary>
        /// Test the matching engine with simulated orders
        /// </summary>
        Task<TestMatchingResponse> TestMatchingAsync(TestMatchingRequest request);
    }
}