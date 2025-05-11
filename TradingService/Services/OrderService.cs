using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

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

        /// <summary>
        /// Initializes a new instance of the OrderService
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">The logger service</param>
        /// <param name="httpClient">HTTP client service for communicating with other services</param>
        /// <param name="configuration">Application configuration</param>
        public OrderService(
            MongoDbConnectionFactory dbFactory,
            ILoggerService logger,
            IHttpClientService httpClient,
            IConfiguration configuration)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _httpClient = httpClient;
            _accountServiceBaseUrl = configuration["ServiceUrls:AccountService"] ?? "http://account:8080";
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

                // In a real implementation, we would notify the matching engine to process the order
                // This is a simplified placeholder
                // await NotifyMatchingEngine(order);

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
        public async Task<List<OrderResponse>> GetOpenOrdersAsync(ObjectId userId, string? symbol = null)
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

                var orders = await orderCollection.Find(filter).ToListAsync();
                return orders.Select(ConvertToOrderResponse).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open orders for user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<OrderHistoryResponse> GetOrderHistoryAsync(ObjectId userId, OrderHistoryRequest request)
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

                return new OrderHistoryResponse
                {
                    Total = (int)total,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Orders = orders.Select(ConvertToOrderResponse).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order history for user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(List<TradeResponse> Trades, int Total)> GetTradeHistoryAsync(
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
                var filter = Builders<Trade>.Filter.Eq(t => t.UserId, userId);

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

                return (trades.Select(ConvertToTradeResponse).ToList(), (int)total);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trade history for user {userId}: {ex.Message}");
                throw;
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
        /// Converts an Order entity to an OrderResponse
        /// </summary>
        /// <param name="order">The order to convert</param>
        /// <returns>OrderResponse representing the order</returns>
        private OrderResponse ConvertToOrderResponse(Order order)
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
                Time = new DateTimeOffset(order.CreatedAt).ToUnixTimeSeconds(),
                UpdateTime = new DateTimeOffset(order.UpdatedAt).ToUnixTimeSeconds(),
                IsWorking = order.IsWorking,
                Fills = order.Trades.Select(ConvertToOrderFill).ToList()
            };
        }

        /// <summary>
        /// Converts a Trade entity to an OrderFill
        /// </summary>
        /// <param name="trade">The trade to convert</param>
        /// <returns>OrderFill representing the trade</returns>
        private OrderFill ConvertToOrderFill(Trade trade)
        {
            return new OrderFill
            {
                Price = trade.Price,
                Quantity = trade.Quantity,
                Commission = 0, // In a real system, calculate this based on user's tier
                CommissionAsset = "USDT", // This might vary depending on the platform
                TradeId = trade.Id.ToString(),
                Time = new DateTimeOffset(trade.CreatedAt).ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// Converts a Trade entity to a TradeResponse
        /// </summary>
        /// <param name="trade">The trade to convert</param>
        /// <returns>TradeResponse representing the trade</returns>
        private TradeResponse ConvertToTradeResponse(Trade trade)
        {
            return new TradeResponse
            {
                Id = trade.Id.ToString(),
                Symbol = trade.Symbol,
                Price = trade.Price,
                Quantity = trade.Quantity,
                Time = new DateTimeOffset(trade.CreatedAt).ToUnixTimeSeconds(),
                IsBuyerMaker = trade.IsBuyerMaker
            };
        }

        #endregion
    }
}