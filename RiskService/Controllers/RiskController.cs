using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommonLib.Models.Risk;
using RiskService.Services;
using CommonLib.Services;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Text.Json;

namespace RiskService.Controllers
{
    /// <summary>
    /// Controller for risk management operations
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class RiskController : ControllerBase
    {
        private readonly IRiskService _riskService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Constructor for RiskController
        /// </summary>
        /// <param name="riskService">Risk service for business logic</param>
        /// <param name="logger">Logger service</param>
        /// <param name="apiLogger">API logging service</param>
        public RiskController(IRiskService riskService, ILoggerService logger, IApiLoggingService apiLogger)
        {
            _riskService = riskService;
            _logger = logger;
            _apiLogger = apiLogger;
        }

        /// <summary>
        /// Get risk status for current user
        /// </summary>
        /// <returns>Risk status information</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(RiskProfileResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRiskStatus()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Extract userId from JWT claims
                var userIdString = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
                {
                    var errorResponse = new { message = "Invalid user ID in token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var riskProfile = await _riskService.GetRiskProfileAsync(userId);
                if (riskProfile == null)
                {
                    var errorResponse = new { message = "Risk profile not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Get active alerts
                var activeAlerts = await _riskService.GetActiveAlertsAsync(userId);

                // Convert to response model
                var response = new RiskProfileResponse
                {
                    UserId = riskProfile.UserId.ToString(),
                    RiskLevel = riskProfile.RiskLevel,
                    RiskScore = 0, // Set default or calculate based on available data
                    TotalPositionValue = 0, // Set default or calculate based on available data
                    MaxPositionValue = 0, // Set default or calculate based on available data
                    DailyTradingVolume = 0, // Set default or calculate based on available data
                    ActiveAlerts = activeAlerts.Select(alert => new RiskAlertResponse
                    {
                        Id = alert.Id.ToString(),
                        UserId = alert.UserId.ToString(),
                        Type = alert.Type,
                        Severity = alert.Severity,
                        Message = alert.Message,
                        IsAcknowledged = alert.IsAcknowledged,
                        AcknowledgedAt = alert.AcknowledgedAt.HasValue ?
                            new DateTimeOffset(alert.AcknowledgedAt.Value).ToUnixTimeMilliseconds() : null,
                        AcknowledgmentComment = alert.AcknowledgmentComment,
                        CreatedAt = new DateTimeOffset(alert.CreatedAt).ToUnixTimeMilliseconds()
                    }).ToList(),
                    UpdatedAt = new DateTimeOffset(riskProfile.UpdatedAt).ToUnixTimeMilliseconds()
                };

                var successResponse = new { data = response, success = true };
                var responseJson = JsonSerializer.Serialize(successResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRiskStatus: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred processing the request", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get trading limits for current user
        /// </summary>
        /// <returns>Trading limits information</returns>
        [HttpGet("limits")]
        [ProducesResponseType(typeof(TradingLimitsResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTradingLimits()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Extract userId from JWT claims
                var userIdString = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
                {
                    var errorResponse = new { message = "Invalid user ID in token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var limits = await _riskService.GetTradingLimitsAsync(userId);
                if (limits == null)
                {
                    var errorResponse = new { message = "Trading limits not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Convert to response model
                var response = new TradingLimitsResponse
                {
                    UserId = userId.ToString(),
                    MaxPositionValue = limits.AssetSpecificLimits.Values.Sum(), // Use sum of asset limits as an approximation
                    MaxDailyVolume = limits.DailyTradingLimit,
                    MaxOrderValue = limits.SingleOrderLimit,
                    MaxLeverage = 5.0m, // Set default or get from limits if available
                    UpdatedAt = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond
                };

                var successResponse = new { data = response, success = true };
                var responseJson = JsonSerializer.Serialize(successResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetTradingLimits: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred processing the request", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get active risk alerts for current user
        /// </summary>
        /// <returns>List of active risk alerts</returns>
        [HttpGet("alerts")]
        [ProducesResponseType(typeof(List<RiskAlertResponse>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetRiskAlerts()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Extract userId from JWT claims
                var userIdString = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
                {
                    var errorResponse = new { message = "Invalid user ID in token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var alerts = await _riskService.GetActiveAlertsAsync(userId);

                // Convert to response models
                var responseList = alerts.Select(alert => new RiskAlertResponse
                {
                    Id = alert.Id.ToString(),
                    UserId = alert.UserId.ToString(),
                    Type = alert.Type,
                    Severity = alert.Severity,
                    Message = alert.Message,
                    IsAcknowledged = alert.IsAcknowledged,
                    AcknowledgedAt = alert.AcknowledgedAt.HasValue ?
                        new DateTimeOffset(alert.AcknowledgedAt.Value).ToUnixTimeMilliseconds() : null,
                    AcknowledgmentComment = alert.AcknowledgmentComment,
                    CreatedAt = new DateTimeOffset(alert.CreatedAt).ToUnixTimeMilliseconds()
                }).ToList();

                var successResponse = new { data = responseList, success = true };
                var responseJson = JsonSerializer.Serialize(successResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRiskAlerts: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred processing the request", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Acknowledge a risk alert
        /// </summary>
        /// <param name="alertId">ID of the alert to acknowledge</param>
        /// <param name="request">Acknowledgment request (optional comment)</param>
        /// <returns>Success or error status</returns>
        [HttpPost("alerts/{alertId}/acknowledge")]
        [ProducesResponseType(typeof(RiskAlertResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AcknowledgeAlert(string alertId, [FromBody] AcknowledgeAlertRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Extract userId from JWT claims
                var userIdString = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
                {
                    var errorResponse = new { message = "Invalid user ID in token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                if (!ObjectId.TryParse(alertId, out var parsedAlertId))
                {
                    var errorResponse = new { message = "Invalid alert ID format", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Set comment if provided in the request
                string? comment = request?.Comment;

                var result = await _riskService.AcknowledgeAlertAsync(parsedAlertId, userId, comment);
                if (result == null)
                {
                    var errorResponse = new { message = "Alert not found or already acknowledged", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Convert to response model
                var response = new RiskAlertResponse
                {
                    Id = result.Id.ToString(),
                    UserId = result.UserId.ToString(),
                    Type = result.Type,
                    Severity = result.Severity,
                    Message = result.Message,
                    IsAcknowledged = result.IsAcknowledged,
                    AcknowledgedAt = result.AcknowledgedAt.HasValue ?
                        new DateTimeOffset(result.AcknowledgedAt.Value).ToUnixTimeMilliseconds() : null,
                    AcknowledgmentComment = result.AcknowledgmentComment,
                    CreatedAt = new DateTimeOffset(result.CreatedAt).ToUnixTimeMilliseconds()
                };

                var successResponse = new { data = response, success = true };
                var responseJson = JsonSerializer.Serialize(successResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AcknowledgeAlert: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred processing the request", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get list of active risk rules
        /// </summary>
        /// <returns>List of active risk rules</returns>
        [HttpGet("rules")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<RiskRuleResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetRiskRules()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var rules = await _riskService.GetActiveRulesAsync();

                // Convert to response models
                var responseList = rules.Select(rule => new RiskRuleResponse
                {
                    Id = rule.Id.ToString(),
                    Name = rule.Name,
                    Description = rule.Description,
                    MaxPositionSize = rule.MaxPositionSize,
                    MaxOrderSize = rule.MaxOrderSize,
                    MaxOrdersPerDay = rule.MaxOrdersPerDay,
                    IsEnabled = rule.IsEnabled,
                    UpdatedAt = new DateTimeOffset(rule.UpdatedAt).ToUnixTimeMilliseconds()
                }).ToList();

                var successResponse = new { data = responseList, success = true };
                var responseJson = JsonSerializer.Serialize(successResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRiskRules: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred processing the request", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }
    }
}