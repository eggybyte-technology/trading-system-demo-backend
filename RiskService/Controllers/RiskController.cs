using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommonLib.Models.Risk;
using RiskService.Services;
using CommonLib.Services;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

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

        /// <summary>
        /// Constructor for RiskController
        /// </summary>
        /// <param name="riskService">Risk service for business logic</param>
        /// <param name="logger">Logger service</param>
        public RiskController(IRiskService riskService, ILoggerService logger)
        {
            _riskService = riskService;
            _logger = logger;
        }

        /// <summary>
        /// Get risk status for current user
        /// </summary>
        /// <returns>Risk status information</returns>
        [HttpGet("status")]
        public async Task<ActionResult<RiskProfile>> GetRiskStatus()
        {
            _logger.LogInformation($"API Request: GET /risk/status | User: {User.FindFirst("userId")?.Value ?? "unknown"}");

            // Extract userId from JWT claims
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid user ID in token");
            }

            var riskProfile = await _riskService.GetRiskProfileAsync(userId);
            if (riskProfile == null)
            {
                return NotFound("Risk profile not found");
            }

            return Ok(riskProfile);
        }

        /// <summary>
        /// Get trading limits for current user
        /// </summary>
        /// <returns>Trading limits information</returns>
        [HttpGet("limits")]
        public async Task<ActionResult<TradingLimits>> GetTradingLimits()
        {
            _logger.LogInformation($"API Request: GET /risk/limits | User: {User.FindFirst("userId")?.Value ?? "unknown"}");

            // Extract userId from JWT claims
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid user ID in token");
            }

            var limits = await _riskService.GetTradingLimitsAsync(userId);
            if (limits == null)
            {
                return NotFound("Trading limits not found");
            }

            return Ok(limits);
        }

        /// <summary>
        /// Get active risk alerts for current user
        /// </summary>
        /// <returns>List of active risk alerts</returns>
        [HttpGet("alerts")]
        public async Task<ActionResult<List<RiskAlert>>> GetRiskAlerts()
        {
            _logger.LogInformation($"API Request: GET /risk/alerts | User: {User.FindFirst("userId")?.Value ?? "unknown"}");

            // Extract userId from JWT claims
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid user ID in token");
            }

            var alerts = await _riskService.GetActiveAlertsAsync(userId);
            return Ok(alerts);
        }

        /// <summary>
        /// Acknowledge a risk alert
        /// </summary>
        /// <param name="alertId">ID of the alert to acknowledge</param>
        /// <returns>Success or error status</returns>
        [HttpPost("alerts/{alertId}/acknowledge")]
        public async Task<ActionResult> AcknowledgeAlert(string alertId)
        {
            _logger.LogInformation($"API Request: POST /risk/alerts/{alertId}/acknowledge | User: {User.FindFirst("userId")?.Value ?? "unknown"}");

            // Extract userId from JWT claims
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid user ID in token");
            }

            if (!ObjectId.TryParse(alertId, out var parsedAlertId))
            {
                return BadRequest("Invalid alert ID format");
            }

            var success = await _riskService.AcknowledgeAlertAsync(parsedAlertId, userId);
            if (!success)
            {
                return NotFound("Alert not found or already acknowledged");
            }

            return Ok(new { message = "Alert acknowledged successfully" });
        }

        /// <summary>
        /// Get list of active risk rules
        /// </summary>
        /// <returns>List of active risk rules</returns>
        [HttpGet("rules")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<RiskRule>>> GetRiskRules()
        {
            _logger.LogInformation($"API Request: GET /risk/rules | User: {User.FindFirst("userId")?.Value ?? "unknown"}");

            var rules = await _riskService.GetActiveRulesAsync();
            return Ok(rules);
        }
    }
}