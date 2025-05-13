using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using TradingService.Services;
using System.Linq;
using System.Text.Json;

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
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

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
        /// <param name="request">Trade history request parameters</param>
        /// <returns>Trade history with pagination</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(TradeHistoryResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTradeHistory([FromQuery] TradeHistoryRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Validate pagination parameters
                if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
                {
                    var errorResponse = new { message = "Invalid pagination parameters", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Get trade history
                var (trades, total) = await _orderService.GetTradeHistoryAsync(
                    userId,
                    request.Symbol,
                    request.StartTime,
                    request.EndTime,
                    request.Page,
                    request.PageSize);

                // Convert Trade objects to TradeResponse objects
                var tradeResponses = trades.Select(t => new TradeResponse
                {
                    Id = t.Id.ToString(),
                    Symbol = t.Symbol,
                    Price = t.Price,
                    Quantity = t.Quantity,
                    Time = new DateTimeOffset(t.CreatedAt).ToUnixTimeMilliseconds(),
                    IsBuyerMaker = t.IsBuyerMaker
                }).ToList();

                // Return paginated result
                var historyResponse = new TradeHistoryResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = total,
                    TotalPages = (int)Math.Ceiling((double)total / request.PageSize),
                    HasNextPage = request.Page * request.PageSize < total,
                    HasPreviousPage = request.Page > 1,
                    Items = tradeResponses
                };

                var response = new { data = historyResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trade history: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while retrieving trade history", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }
    }
}