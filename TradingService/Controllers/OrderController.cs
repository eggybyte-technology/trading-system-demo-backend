using System;
using System.Collections.Generic;
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
    /// Controller for order operations
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("order")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;

        /// <summary>
        /// Initializes a new instance of the OrderController
        /// </summary>
        public OrderController(
            IOrderService orderService,
            ILoggerService logger,
            IApiLoggingService apiLogger)
        {
            _orderService = orderService;
            _logger = logger;
            _apiLogger = apiLogger;
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <returns>Created order details</returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Create the order
                var order = await _orderService.CreateOrderAsync(userId, request);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid order request: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating the order" });
            }
        }

        /// <summary>
        /// Cancels an existing order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Success or failure message</returns>
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Parse order ID
                if (!ObjectId.TryParse(orderId, out var orderObjectId))
                    return BadRequest(new { message = "Invalid order ID format" });

                // Cancel the order
                var result = await _orderService.CancelOrderAsync(orderObjectId, userId);

                if (result)
                {
                    return Ok(new { message = "Order canceled successfully" });
                }
                else
                {
                    return NotFound(new { message = "Order not found or already filled/canceled" });
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cannot cancel order: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling order: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while canceling the order" });
            }
        }

        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Order details</returns>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(string orderId)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Parse order ID
                if (!ObjectId.TryParse(orderId, out var orderObjectId))
                    return BadRequest(new { message = "Invalid order ID format" });

                // Get the order
                var order = await _orderService.GetOrderAsync(orderObjectId, userId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving the order" });
            }
        }

        /// <summary>
        /// Gets open orders for the current user
        /// </summary>
        /// <param name="symbol">Optional symbol filter</param>
        /// <returns>List of open orders</returns>
        [HttpGet("open")]
        public async Task<IActionResult> GetOpenOrders([FromQuery] string? symbol = null)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Get open orders
                var orders = await _orderService.GetOpenOrdersAsync(userId, symbol);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open orders: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving open orders" });
            }
        }

        /// <summary>
        /// Gets order history for the current user
        /// </summary>
        /// <param name="request">Order history request parameters</param>
        /// <returns>Order history with pagination</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetOrderHistory([FromQuery] OrderHistoryRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Get order history
                var history = await _orderService.GetOrderHistoryAsync(userId, request);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order history: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving order history" });
            }
        }
    }
}