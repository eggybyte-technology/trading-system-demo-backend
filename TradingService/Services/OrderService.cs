using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using CommonLib.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using CommonLib.Api;
using System.Net.Http.Json;
using CommonLib.Models.Account;
using CommonLib.Models.Market;
using TradingService.Repositories;

namespace TradingService.Services
{
    /// <summary>
    /// Service for order management operations
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;
        private readonly IHttpClientService _httpClient;
        private readonly string _accountServiceBaseUrl;
        private readonly CommonLib.Api.AccountService _accountService;
        private readonly string _serviceAuthToken;
        private readonly IConfiguration _configuration;
        private readonly IWebSocketService _webSocketService;

        /// <summary>
        /// Initializes a new instance of the OrderService
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">The logger service</param>
        /// <param name="httpClient">HTTP client service for communicating with other services</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="accountService">Account service client for API calls</param>
        /// <param name="webSocketService">WebSocket service for real-time updates</param>
        public OrderService(
            MongoDbConnectionFactory dbFactory,
            ILoggerService logger,
            IHttpClientService httpClient,
            IConfiguration configuration,
            CommonLib.Api.AccountService accountService,
            IWebSocketService webSocketService)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _httpClient = httpClient;
            _accountServiceBaseUrl = configuration["ServiceUrls:AccountService"] ?? "http://account:8080";
            _accountService = accountService;
            _configuration = configuration;
            _webSocketService = webSocketService;

            // Get system token for inter-service communication
            var jwtSection = configuration.GetSection("JwtSettings");
            _serviceAuthToken = jwtSection["ServiceToken"] ?? "default-service-token";
        }

        /// <inheritdoc/>
        public async Task<Order> CreateOrderAsync(ObjectId userId, CreateOrderRequest request)
        {
            try
            {
                _logger.LogInformation($"Creating order for user {userId}: {request.Side} {request.Quantity} {request.Symbol} at {request.Price}");

                // Validate request
                ValidateOrderRequest(request);

                // Create the order entity
                var order = new Order
                {
                    UserId = userId,
                    Symbol = request.Symbol,
                    Side = request.Side,
                    Type = request.Type,
                    TimeInForce = request.TimeInForce,
                    Price = request.Price ?? 0, // Market orders don't require price
                    OriginalQuantity = request.Quantity,
                    ExecutedQuantity = 0,
                    StopPrice = request.StopPrice,
                    IcebergQuantity = request.IcebergQty,
                    Status = "NEW",
                    IsWorking = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save the order
                var orderCollection = _dbFactory.GetCollection<Order>();
                await orderCollection.InsertOneAsync(order);

                // Notify MarketDataService to update OrderBook
                await NotifyOrderBookUpdate(order);

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CancelOrderAsync(ObjectId orderId, ObjectId userId)
        {
            try
            {
                var orderCollection = _dbFactory.GetCollection<Order>();

                // First, check if the order exists and is locked
                var order = await orderCollection.Find(o => o.Id == orderId && o.UserId == userId).FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found or does not belong to user {userId}");
                    return false;
                }

                if (order.IsLocked)
                {
                    _logger.LogWarning($"Order {orderId} is currently locked for matching process, cannot cancel");
                    throw new InvalidOperationException("Order is currently locked for matching process and cannot be canceled. Please try again later.");
                }

                // Proceed with cancellation if not locked
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Id, orderId),
                    Builders<Order>.Filter.Eq(o => o.UserId, userId),
                    Builders<Order>.Filter.Eq(o => o.IsLocked, false) // Only cancel if not locked
                );

                var update = Builders<Order>.Update
                    .Set(o => o.Status, "CANCELED")
                    .Set(o => o.IsWorking, false)
                    .Set(o => o.UpdatedAt, DateTime.UtcNow);

                var result = await orderCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation($"Order {orderId} successfully canceled");
                    // In a real implementation, notify matching engine about cancellation
                    // await NotifyMatchingEngineOfCancellation(orderId);
                    return true;
                }

                return false;
            }
            catch (InvalidOperationException)
            {
                // Rethrow InvalidOperationException for locked orders
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling order {orderId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Order?> GetOrderAsync(ObjectId orderId, ObjectId userId)
        {
            try
            {
                var orderCollection = _dbFactory.GetCollection<Order>();
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Id, orderId),
                    Builders<Order>.Filter.Eq(o => o.UserId, userId)
                );

                return await orderCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order {orderId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Order>> GetOpenOrdersAsync(ObjectId userId, string? symbol = null)
        {
            try
            {
                var orderCollection = _dbFactory.GetCollection<Order>();
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.UserId, userId),
                    Builders<Order>.Filter.Eq(o => o.Status, "NEW")
                );

                if (!string.IsNullOrEmpty(symbol))
                {
                    filter = Builders<Order>.Filter.And(filter, Builders<Order>.Filter.Eq(o => o.Symbol, symbol));
                }

                return await orderCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open orders for user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(List<Order> Orders, int Total)> GetOrderHistoryAsync(ObjectId userId, OrderHistoryRequest request)
        {
            try
            {
                DateTime? startTime = request.StartTime.HasValue
                    ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(request.StartTime.Value)
                    : null;

                DateTime? endTime = request.EndTime.HasValue
                    ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(request.EndTime.Value)
                    : null;

                var orderCollection = _dbFactory.GetCollection<Order>();
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.UserId, userId)
                );

                if (startTime.HasValue)
                {
                    filter = Builders<Order>.Filter.And(filter, Builders<Order>.Filter.Gte(o => o.CreatedAt, startTime));
                }

                if (endTime.HasValue)
                {
                    filter = Builders<Order>.Filter.And(filter, Builders<Order>.Filter.Lte(o => o.CreatedAt, endTime));
                }

                if (!string.IsNullOrEmpty(request.Symbol))
                {
                    filter = Builders<Order>.Filter.And(filter, Builders<Order>.Filter.Eq(o => o.Symbol, request.Symbol));
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    filter = Builders<Order>.Filter.And(filter, Builders<Order>.Filter.Eq(o => o.Status, request.Status));
                }

                var total = await orderCollection.CountDocumentsAsync(filter);
                var orders = await orderCollection.Find(filter)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Limit(request.PageSize)
                    .ToListAsync();

                return (orders, (int)total);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order history for user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(List<Trade> Trades, int Total)> GetTradeHistoryAsync(
            ObjectId userId,
            string? symbol = null,
            long? startTime = null,
            long? endTime = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                DateTime? startDateTime = startTime.HasValue
                    ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(startTime.Value)
                    : null;

                DateTime? endDateTime = endTime.HasValue
                    ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(endTime.Value)
                    : null;

                var tradeCollection = _dbFactory.GetCollection<Trade>();

                // Create filter for trades where user is either buyer or seller
                var filter = Builders<Trade>.Filter.Or(
                    Builders<Trade>.Filter.Eq(t => t.BuyerUserId, userId),
                    Builders<Trade>.Filter.Eq(t => t.SellerUserId, userId)
                );

                if (!string.IsNullOrEmpty(symbol))
                {
                    filter = Builders<Trade>.Filter.And(filter, Builders<Trade>.Filter.Eq(t => t.Symbol, symbol));
                }

                if (startDateTime.HasValue)
                {
                    filter = Builders<Trade>.Filter.And(filter, Builders<Trade>.Filter.Gte(t => t.CreatedAt, startDateTime));
                }

                if (endDateTime.HasValue)
                {
                    filter = Builders<Trade>.Filter.And(filter, Builders<Trade>.Filter.Lte(t => t.CreatedAt, endDateTime));
                }

                var total = await tradeCollection.CountDocumentsAsync(filter);
                var trades = await tradeCollection.Find(filter)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                // Set the UserId property for convenience in response mapping
                foreach (var trade in trades)
                {
                    trade.UserId = userId;
                }

                return (trades, (int)total);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trade history for user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LockOrderResponse> LockOrderAsync(LockOrderRequest request)
        {
            try
            {
                if (!ObjectId.TryParse(request.OrderId, out var orderObjectId))
                {
                    return new LockOrderResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid order ID format",
                        LockId = request.LockId
                    };
                }

                var orderCollection = _dbFactory.GetCollection<Order>();

                // Find the order and make sure it's not already locked
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Id, orderObjectId),
                    Builders<Order>.Filter.Eq(o => o.Status, "NEW"), // Only lock NEW orders
                    Builders<Order>.Filter.Eq(o => o.IsLocked, false) // Not already locked
                );

                // Update to set the lock
                var update = Builders<Order>.Update
                    .Set(o => o.IsLocked, true)
                    .Set(o => o.LockId, request.LockId)
                    .Set(o => o.LockExpiration, DateTime.UtcNow.AddSeconds(request.TimeoutSeconds))
                    .Set(o => o.UpdatedAt, DateTime.UtcNow);

                var result = await orderCollection.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<Order> { ReturnDocument = ReturnDocument.After }
                );

                if (result == null)
                {
                    // Check if the order exists at all
                    var orderExists = await orderCollection.Find(o => o.Id == orderObjectId).AnyAsync();

                    if (!orderExists)
                    {
                        return new LockOrderResponse
                        {
                            Success = false,
                            ErrorMessage = "Order not found",
                            LockId = request.LockId
                        };
                    }

                    // Order exists but couldn't be locked
                    return new LockOrderResponse
                    {
                        Success = false,
                        ErrorMessage = "Order is already locked or not in a lockable state",
                        LockId = request.LockId
                    };
                }

                // Convert the Order to OrderResponse
                var orderResponse = new OrderResponse
                {
                    OrderId = result.Id.ToString(),
                    Symbol = result.Symbol,
                    UserId = result.UserId.ToString(),
                    Price = result.Price,
                    OrigQty = result.OriginalQuantity,
                    ExecutedQty = result.ExecutedQuantity,
                    Status = result.Status,
                    TimeInForce = result.TimeInForce,
                    Type = result.Type,
                    Side = result.Side,
                    StopPrice = result.StopPrice,
                    IcebergQty = result.IcebergQuantity,
                    Time = new DateTimeOffset(result.CreatedAt).ToUnixTimeMilliseconds(),
                    UpdateTime = new DateTimeOffset(result.UpdatedAt).ToUnixTimeMilliseconds(),
                    IsWorking = result.Status != "CANCELED" && result.Status != "FILLED" && result.Status != "REJECTED"
                };

                return new LockOrderResponse
                {
                    Success = true,
                    LockId = request.LockId,
                    Order = orderResponse,
                    ExpirationTimestamp = new DateTimeOffset(result.LockExpiration.Value).ToUnixTimeMilliseconds()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error locking order {request.OrderId}: {ex.Message}");
                return new LockOrderResponse
                {
                    Success = false,
                    ErrorMessage = $"Error locking order: {ex.Message}",
                    LockId = request.LockId
                };
            }
        }

        /// <inheritdoc/>
        public async Task<UnlockOrderResponse> UnlockOrderAsync(UnlockOrderRequest request)
        {
            try
            {
                if (!ObjectId.TryParse(request.OrderId, out var orderObjectId))
                {
                    return new UnlockOrderResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid order ID format"
                    };
                }

                var orderCollection = _dbFactory.GetCollection<Order>();

                // Find the order and make sure it's locked with the correct lock ID
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Id, orderObjectId),
                    Builders<Order>.Filter.Eq(o => o.IsLocked, true),
                    Builders<Order>.Filter.Eq(o => o.LockId, request.LockId)
                );

                // Update to release the lock
                var update = Builders<Order>.Update
                    .Set(o => o.IsLocked, false)
                    .Set(o => o.LockId, null)
                    .Set(o => o.LockExpiration, null)
                    .Set(o => o.UpdatedAt, DateTime.UtcNow);

                var result = await orderCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    // Check if the order exists at all
                    var orderExists = await orderCollection.Find(o => o.Id == orderObjectId).AnyAsync();

                    if (!orderExists)
                    {
                        return new UnlockOrderResponse
                        {
                            Success = false,
                            ErrorMessage = "Order not found"
                        };
                    }

                    // Order exists but couldn't be unlocked with this lock ID
                    return new UnlockOrderResponse
                    {
                        Success = false,
                        ErrorMessage = "Order is not locked or lock ID doesn't match"
                    };
                }

                return new UnlockOrderResponse
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unlocking order {request.OrderId}: {ex.Message}");
                return new UnlockOrderResponse
                {
                    Success = false,
                    ErrorMessage = $"Error unlocking order: {ex.Message}"
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateOrderStatusAsync(UpdateOrderStatusRequest request)
        {
            try
            {
                if (!ObjectId.TryParse(request.OrderId, out var orderObjectId))
                {
                    return false;
                }

                var orderCollection = _dbFactory.GetCollection<Order>();

                // Create filter based on whether a lock ID is provided
                FilterDefinition<Order> filter;
                if (!string.IsNullOrEmpty(request.LockId))
                {
                    // If lock ID is provided, ensure it matches
                    filter = Builders<Order>.Filter.And(
                        Builders<Order>.Filter.Eq(o => o.Id, orderObjectId),
                        Builders<Order>.Filter.Eq(o => o.IsLocked, true),
                        Builders<Order>.Filter.Eq(o => o.LockId, request.LockId)
                    );
                }
                else
                {
                    // If no lock ID, just check the ID
                    filter = Builders<Order>.Filter.Eq(o => o.Id, orderObjectId);
                }

                // Update the order status and quantities
                var update = Builders<Order>.Update
                    .Set(o => o.Status, request.Status)
                    .Set(o => o.ExecutedQuantity, request.ExecutedQuantity)
                    .Set(o => o.CumulativeQuoteQuantity, request.CumulativeQuoteQuantity)
                    .Set(o => o.UpdatedAt, DateTime.UtcNow);

                // If we're updating based on a lock, also release the lock
                if (!string.IsNullOrEmpty(request.LockId))
                {
                    update = update
                        .Set(o => o.IsLocked, false)
                        .Set(o => o.LockId, null)
                        .Set(o => o.LockExpiration, null);
                }

                // If the order is completely filled or canceled, mark it as not working
                if (request.Status == "FILLED" || request.Status == "CANCELED" || request.Status == "REJECTED")
                {
                    update = update.Set(o => o.IsWorking, false);
                }

                var result = await orderCollection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status for {request.OrderId}: {ex.Message}");
                return false;
            }
        }

        #region Private Helpers

        /// <summary>
        /// Validates an order request
        /// </summary>
        /// <param name="request">The order request to validate</param>
        private void ValidateOrderRequest(CreateOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Symbol))
                throw new ArgumentException("Symbol is required");

            if (string.IsNullOrWhiteSpace(request.Side) || (request.Side != "BUY" && request.Side != "SELL"))
                throw new ArgumentException("Side must be BUY or SELL");

            if (string.IsNullOrWhiteSpace(request.Type) || !IsValidOrderType(request.Type))
                throw new ArgumentException("Invalid order type");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");

            // LIMIT orders must have a price
            if (request.Type == "LIMIT" && (!request.Price.HasValue || request.Price <= 0))
                throw new ArgumentException("Price is required for LIMIT orders and must be greater than zero");

            // STOP_LOSS and STOP_LOSS_LIMIT orders must have a stop price
            if ((request.Type == "STOP_LOSS" || request.Type == "STOP_LOSS_LIMIT") && (!request.StopPrice.HasValue || request.StopPrice <= 0))
                throw new ArgumentException("Stop price is required for STOP_LOSS orders and must be greater than zero");

            // ICEBERG orders must have an iceberg quantity
            if (request.IcebergQty.HasValue && request.IcebergQty <= 0)
                throw new ArgumentException("Iceberg quantity must be greater than zero");

            // Validate time in force
            if (!IsValidTimeInForce(request.TimeInForce))
                throw new ArgumentException("Invalid time in force");
        }

        /// <summary>
        /// Checks if an order type is valid
        /// </summary>
        /// <param name="type">The order type to check</param>
        /// <returns>True if valid</returns>
        private bool IsValidOrderType(string type)
        {
            var validTypes = new[] { "LIMIT", "MARKET", "STOP_LOSS", "STOP_LOSS_LIMIT", "TAKE_PROFIT", "TAKE_PROFIT_LIMIT" };
            return validTypes.Contains(type);
        }

        /// <summary>
        /// Checks if a time in force value is valid
        /// </summary>
        /// <param name="timeInForce">The time in force to check</param>
        /// <returns>True if valid</returns>
        private bool IsValidTimeInForce(string timeInForce)
        {
            var validValues = new[] { "GTC", "IOC", "FOK" };
            return validValues.Contains(timeInForce);
        }

        /// <summary>
        /// Notifies MarketDataService to update the OrderBook with the new order
        /// </summary>
        /// <param name="order">The order that was created</param>
        private async Task NotifyOrderBookUpdate(Order order)
        {
            try
            {
                _logger.LogInformation($"Notifying MarketDataService of new order {order.Id} for {order.Symbol}");

                // Create order book update request
                var updateRequest = new OrderBookUpdateRequest
                {
                    Symbol = order.Symbol
                };

                // Prepare the bids/asks based on order side
                if (order.Side.ToUpper() == "BUY")
                {
                    updateRequest.Bids.Add(new List<decimal> { order.Price, order.OriginalQuantity - order.ExecutedQuantity });
                }
                else
                {
                    updateRequest.Asks.Add(new List<decimal> { order.Price, order.OriginalQuantity - order.ExecutedQuantity });
                }

                // Use the MarketDataService client
                var marketDataService = new CommonLib.Api.MarketDataService(_configuration);
                var response = await marketDataService.UpdateOrderBookAsync(_serviceAuthToken, updateRequest);

                if (!response.Success)
                {
                    _logger.LogWarning($"Failed to update order book for {order.Symbol}: {response.Message}");
                }
                else
                {
                    _logger.LogInformation($"Successfully updated order book for {order.Symbol}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying MarketDataService: {ex.Message}");
                // Continue despite error - this is a non-critical operation
            }
        }

        #endregion
    }
}