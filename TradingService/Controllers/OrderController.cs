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
using System.Text.Json;

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
        private readonly IWebSocketService _webSocketService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Initializes a new instance of the OrderController
        /// </summary>
        public OrderController(
            IOrderService orderService,
            ILoggerService logger,
            IApiLoggingService apiLogger,
            IWebSocketService webSocketService)
        {
            _orderService = orderService;
            _logger = logger;
            _apiLogger = apiLogger;
            _webSocketService = webSocketService;
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <returns>Created order details</returns>
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
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

                // Create the order
                var order = await _orderService.CreateOrderAsync(userId, request);

                // Convert to response model
                var orderResponse = CreateOrderResponse(order);

                var response = new { data = orderResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid order request: {ex.Message}");
                var errorResponse = new { message = ex.Message, success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while creating the order", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Cancels an existing order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Order cancellation response</returns>
        [HttpDelete("{orderId}")]
        [ProducesResponseType(typeof(CancelOrderResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CancelOrder(string orderId)
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

                // Parse order ID
                if (!ObjectId.TryParse(orderId, out var orderObjectId))
                {
                    var errorResponse = new { message = "Invalid order ID format", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Cancel the order
                var result = await _orderService.CancelOrderAsync(orderObjectId, userId);
                var order = await _orderService.GetOrderAsync(orderObjectId, userId);

                if (result)
                {
                    var cancelResponse = new CancelOrderResponse
                    {
                        OrderId = orderId,
                        Symbol = order?.Symbol ?? string.Empty,
                        Message = "Order canceled successfully",
                        Success = true
                    };
                    var response = new { data = cancelResponse, success = true };
                    var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Ok(response);
                }
                else
                {
                    var cancelResponse = new CancelOrderResponse
                    {
                        OrderId = orderId,
                        Symbol = order?.Symbol ?? string.Empty,
                        Message = "Order not found or already filled/canceled",
                        Success = false
                    };
                    var errorResponse = new { data = cancelResponse, success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cannot cancel order: {ex.Message}");
                var errorResponse = new { message = ex.Message, success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling order: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while canceling the order", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Order details</returns>
        [HttpGet("{orderId}")]
        [ProducesResponseType(typeof(OrderResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetOrder(string orderId)
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

                // Parse order ID
                if (!ObjectId.TryParse(orderId, out var orderObjectId))
                {
                    var errorResponse = new { message = "Invalid order ID format", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Get the order
                var order = await _orderService.GetOrderAsync(orderObjectId, userId);

                if (order == null)
                {
                    var errorResponse = new { message = "Order not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Convert to response model
                var orderResponse = CreateOrderResponse(order);

                var response = new { data = orderResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while retrieving the order", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets open orders for the current user
        /// </summary>
        /// <param name="request">Open orders request parameters</param>
        /// <returns>List of open orders</returns>
        [HttpGet("open")]
        [ProducesResponseType(typeof(OpenOrdersResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetOpenOrders([FromQuery] OpenOrdersRequest request)
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

                // Get open orders
                var orders = await _orderService.GetOpenOrdersAsync(userId, request.Symbol);

                // Convert to response models
                var orderResponses = orders.Select(order => CreateOrderResponse(order, true)).ToList();

                var openOrdersResponse = new OpenOrdersResponse
                {
                    Orders = orderResponses
                };

                var response = new { data = openOrdersResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving open orders: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while retrieving open orders", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets order history for the current user
        /// </summary>
        /// <param name="request">Order history request parameters</param>
        /// <returns>Order history with pagination</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(OrderHistoryResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetOrderHistory([FromQuery] OrderHistoryRequest request)
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

                // Get order history
                var (orders, total) = await _orderService.GetOrderHistoryAsync(userId, request);

                // Convert to response models
                var orderResponses = orders.Select(order => CreateOrderResponse(order)).ToList();

                // Return paginated result
                var historyResponse = new OrderHistoryResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Total = total,
                    Orders = orderResponses
                };

                var response = new { data = historyResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order history: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while retrieving order history", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Lock an order for the matching process
        /// </summary>
        [Authorize(Roles = "Admin,Service")]
        [HttpPost("lock")]
        public async Task<IActionResult> LockOrder([FromBody] LockOrderRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _orderService.LockOrderAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error locking order: {ex.Message}");

                var errorResponse = new { message = "Failed to lock order", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Unlock a previously locked order
        /// </summary>
        [Authorize(Roles = "Admin,Service")]
        [HttpPost("unlock")]
        public async Task<IActionResult> UnlockOrder([FromBody] UnlockOrderRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _orderService.UnlockOrderAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unlocking order: {ex.Message}");

                var errorResponse = new { message = "Failed to unlock order", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Update order status after matching
        /// </summary>
        [Authorize(Roles = "Admin,Service")]
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status: {ex.Message}");

                var errorResponse = new { message = "Failed to update order status", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Helper method to create an OrderResponse from an Order
        /// </summary>
        private OrderResponse CreateOrderResponse(Order order, bool? forceWorking = null)
        {
            return new OrderResponse
            {
                OrderId = order.Id.ToString(),
                Symbol = order.Symbol,
                UserId = order.UserId.ToString(),
                Price = order.Price,
                OrigQty = order.OriginalQuantity,
                ExecutedQty = order.ExecutedQuantity,
                Status = order.Status,
                TimeInForce = order.TimeInForce,
                Type = order.Type,
                Side = order.Side,
                StopPrice = order.StopPrice,
                IcebergQty = order.IcebergQuantity,
                Time = new DateTimeOffset(order.CreatedAt).ToUnixTimeMilliseconds(),
                UpdateTime = new DateTimeOffset(order.UpdatedAt).ToUnixTimeMilliseconds(),
                IsWorking = forceWorking ?? order.Status != "CANCELED" && order.Status != "FILLED" && order.Status != "REJECTED",
                Fills = order.Trades.Select(trade => new OrderFill
                {
                    Price = trade.Price,
                    Quantity = trade.Quantity,
                    Commission = trade.UserId == order.UserId ?
                        (order.Side == "BUY" ? trade.BuyerFee : trade.SellerFee) : 0,
                    CommissionAsset = trade.UserId == order.UserId ?
                        (order.Side == "BUY" ? trade.BuyerFeeAsset : trade.SellerFeeAsset) : "USDT",
                    TradeId = trade.Id.ToString(),
                    Time = new DateTimeOffset(trade.CreatedAt).ToUnixTimeMilliseconds()
                }).ToList()
            };
        }
    }
}