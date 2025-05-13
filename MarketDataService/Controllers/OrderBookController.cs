using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommonLib.Models.Market;
using CommonLib.Services;
using MarketDataService.Services;
using MarketDataService.Repositories;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace MarketDataService.Controllers
{
    /// <summary>
    /// Controller for OrderBook operations
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("orderbook")]
    public class OrderBookController : ControllerBase
    {
        private readonly IMarketService _marketService;
        private readonly IOrderBookRepository _orderBookRepository;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly IWebSocketService _webSocketService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Initializes a new instance of the OrderBookController
        /// </summary>
        public OrderBookController(
            IMarketService marketService,
            IOrderBookRepository orderBookRepository,
            ILoggerService logger,
            IApiLoggingService apiLogger,
            IWebSocketService webSocketService)
        {
            _marketService = marketService;
            _orderBookRepository = orderBookRepository;
            _logger = logger;
            _apiLogger = apiLogger;
            _webSocketService = webSocketService;
        }

        /// <summary>
        /// Updates the order book with new order data
        /// </summary>
        /// <param name="request">Order book update request</param>
        /// <returns>Success response</returns>
        [HttpPost("update")]
        [ProducesResponseType(typeof(OrderBookUpdateResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateOrderBook([FromBody] OrderBookUpdateRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrEmpty(request.Symbol))
                {
                    var errorResponse = new OrderBookUpdateResponse { Success = false, Message = "Symbol is required" };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Get current order book
                var orderBook = await _orderBookRepository.GetOrderBookBySymbolAsync(request.Symbol);

                if (orderBook == null)
                {
                    // Create new order book if not exists
                    orderBook = new OrderBook
                    {
                        Symbol = request.Symbol,
                        UpdatedAt = DateTime.UtcNow,
                        Bids = new List<PriceLevel>(),
                        Asks = new List<PriceLevel>()
                    };
                }

                // Convert to WebSocketDepthData for consistency with existing methods
                var depthData = new WebSocketDepthData
                {
                    Symbol = request.Symbol,
                    Bids = request.Bids,
                    Asks = request.Asks
                };

                // Update the order book with new data
                await UpdateOrderBookData(orderBook, depthData);

                // Save the updated order book
                await _orderBookRepository.UpsertOrderBookAsync(orderBook);

                // Publish real-time update to WebSocket clients
                await _webSocketService.PublishDepthUpdate(request.Symbol, depthData);

                var response = new OrderBookUpdateResponse { Success = true, Message = "Order book updated successfully" };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order book: {ex.Message}");
                var errorResponse = new OrderBookUpdateResponse { Success = false, Message = "An error occurred while updating order book data" };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Updates the order book data with new bids and asks
        /// </summary>
        /// <param name="orderBook">The order book to update</param>
        /// <param name="depthData">New depth data</param>
        private async Task UpdateOrderBookData(OrderBook orderBook, WebSocketDepthData depthData)
        {
            orderBook.UpdatedAt = DateTime.UtcNow;

            // Update bids
            foreach (var bid in depthData.Bids)
            {
                if (bid.Count < 2) continue;
                decimal price = bid[0];
                decimal quantity = bid[1];

                // Find existing price level
                var existingLevel = orderBook.Bids.FirstOrDefault(b => b.Price == price);

                if (existingLevel != null)
                {
                    // Update quantity or remove if zero
                    if (quantity > 0)
                    {
                        existingLevel.Quantity = quantity;
                    }
                    else
                    {
                        orderBook.Bids.Remove(existingLevel);
                    }
                }
                else if (quantity > 0)
                {
                    // Add new price level
                    orderBook.Bids.Add(new PriceLevel { Price = price, Quantity = quantity });
                }
            }

            // Update asks
            foreach (var ask in depthData.Asks)
            {
                if (ask.Count < 2) continue;
                decimal price = ask[0];
                decimal quantity = ask[1];

                // Find existing price level
                var existingLevel = orderBook.Asks.FirstOrDefault(a => a.Price == price);

                if (existingLevel != null)
                {
                    // Update quantity or remove if zero
                    if (quantity > 0)
                    {
                        existingLevel.Quantity = quantity;
                    }
                    else
                    {
                        orderBook.Asks.Remove(existingLevel);
                    }
                }
                else if (quantity > 0)
                {
                    // Add new price level
                    orderBook.Asks.Add(new PriceLevel { Price = price, Quantity = quantity });
                }
            }

            // Sort bids (descending) and asks (ascending)
            orderBook.Bids = orderBook.Bids.OrderByDescending(b => b.Price).ToList();
            orderBook.Asks = orderBook.Asks.OrderBy(a => a.Price).ToList();
        }
    }
}