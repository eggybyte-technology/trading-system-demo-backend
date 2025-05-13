using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.MatchMaking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommonLib.Services;
using MatchMakingService.Services;
using System.Text.Json;

namespace MatchMakingService.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("matchmaking")]
    public class MatchMakingController : ControllerBase
    {
        private readonly IMatchMakingService _matchMakingService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        public MatchMakingController(
            IMatchMakingService matchMakingService,
            ILoggerService logger,
            IApiLoggingService apiLogger)
        {
            _matchMakingService = matchMakingService;
            _logger = logger;
            _apiLogger = apiLogger;
        }

        /// <summary>
        /// Get the current status of the matching engine
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var status = await _matchMakingService.GetStatusAsync();
                var responseObject = new { data = status, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving matching engine status: {ex.Message}");

                var errorResponse = new { message = "Failed to retrieve matching engine status", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Manually trigger a matching cycle
        /// </summary>
        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerMatching([FromBody] TriggerMatchingRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _matchMakingService.TriggerMatchingAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error triggering matching cycle: {ex.Message}");

                var errorResponse = new { message = "Failed to trigger matching cycle", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get matching history
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] MatchHistoryRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var history = await _matchMakingService.GetHistoryAsync(request);
                var responseObject = new { data = history, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving matching history: {ex.Message}");

                var errorResponse = new { message = "Failed to retrieve matching history", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get current matching engine settings
        /// </summary>
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var settings = await _matchMakingService.GetSettingsAsync();
                var responseObject = new { data = settings, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving matching settings: {ex.Message}");

                var errorResponse = new { message = "Failed to retrieve matching settings", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Update matching engine settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateMatchingSettingsRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var settings = await _matchMakingService.UpdateSettingsAsync(request);
                var responseObject = new { data = settings, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating matching settings: {ex.Message}");

                var errorResponse = new { message = "Failed to update matching settings", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get matching statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var stats = await _matchMakingService.GetStatsAsync();
                var responseObject = new { data = stats, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving matching stats: {ex.Message}");

                var errorResponse = new { message = "Failed to retrieve matching stats", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Test the matching engine with simulated orders
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestMatching([FromBody] TestMatchingRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _matchMakingService.TestMatchingAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error testing matching engine: {ex.Message}");

                var errorResponse = new { message = "Failed to test matching engine", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }
    }
}