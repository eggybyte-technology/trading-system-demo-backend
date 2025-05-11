using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using TradingService.Services;
using System.Linq;

namespace TradingService.Controllers
{
    /// <summary>
    /// Controller for trade operations
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("trade")]
    public class TradeController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;

        /// <summary>
        /// Initializes a new instance of the TradeController
        /// </summary>
        public TradeController(
            IOrderService orderService,
            ILoggerService logger,
            IApiLoggingService apiLogger)
        {
            _orderService = orderService;
            _logger = logger;
            _apiLogger = apiLogger;
        }

        /// <summary>
        /// Gets trade history for the current user
        /// </summary>
        /// <param name="symbol">Optional symbol filter</param>
        /// <param name="startTime">Optional start time filter (Unix timestamp in seconds)</param>
        /// <param name="endTime">Optional end time filter (Unix timestamp in seconds)</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Trade history with pagination</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetTradeHistory(
            [FromQuery] string? symbol = null,
            [FromQuery] long? startTime = null,
            [FromQuery] long? endTime = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Validate pagination parameters
                if (page < 1 || pageSize < 1 || pageSize > 100)
                    return BadRequest(new { message = "Invalid pagination parameters" });

                // Get trade history
                var (trades, total) = await _orderService.GetTradeHistoryAsync(
                    userId,
                    symbol,
                    startTime,
                    endTime,
                    page,
                    pageSize);

                // Return paginated result
                return Ok(new
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    Items = trades
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trade history: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving trade history" });
            }
        }
    }
}