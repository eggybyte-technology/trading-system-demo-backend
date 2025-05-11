using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TradingService.Repositories
{
    /// <summary>
    /// Repository for order operations
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly IMongoCollection<Order> _orders;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the OrderRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">The logger service</param>
        public OrderRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory;
            _orders = _dbFactory.GetCollection<Order>();
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Order?> GetByIdAsync(ObjectId id)
        {
            try
            {
                return await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order by ID: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Order?> GetByClientOrderIdAsync(ObjectId userId)
        {
            try
            {
                return await _orders.Find(o => o.UserId == userId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order by client order ID: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Order> CreateAsync(Order order)
        {
            try
            {
                // Set timestamps
                order.CreatedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                await _orders.InsertOneAsync(order);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(Order order)
        {
            try
            {
                // Update timestamp
                order.UpdatedAt = DateTime.UtcNow;

                var result = await _orders.ReplaceOneAsync(
                    filter: o => o.Id == order.Id,
                    replacement: order);

                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CancelAsync(ObjectId id)
        {
            try
            {
                // Get the order first
                var order = await GetByIdAsync(id);
                if (order == null)
                    return false;

                // Only cancel if it's not already filled or canceled
                if (order.Status == "FILLED" || order.Status == "CANCELED")
                    return false;

                // Update order status
                order.Status = "CANCELED";
                order.IsWorking = false;
                order.UpdatedAt = DateTime.UtcNow;

                return await UpdateAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling order: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Order>> GetOpenOrdersAsync(ObjectId userId, string? symbol = null)
        {
            try
            {
                var builder = Builders<Order>.Filter;
                var filter = builder.Eq(o => o.UserId, userId) &
                             builder.In(o => o.Status, new[] { "NEW", "PARTIALLY_FILLED" }) &
                             builder.Eq(o => o.IsWorking, true);

                if (!string.IsNullOrEmpty(symbol))
                {
                    filter &= builder.Eq(o => o.Symbol, symbol);
                }

                return await _orders.Find(filter)
                    .SortByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open orders: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(List<Order> Orders, int Total)> GetOrderHistoryAsync(
            ObjectId userId,
            string? symbol = null,
            string? status = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var builder = Builders<Order>.Filter;
                var filter = builder.Eq(o => o.UserId, userId);

                if (!string.IsNullOrEmpty(symbol))
                {
                    filter &= builder.Eq(o => o.Symbol, symbol);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    filter &= builder.Eq(o => o.Status, status);
                }

                if (startTime.HasValue)
                {
                    filter &= builder.Gte(o => o.CreatedAt, startTime.Value);
                }

                if (endTime.HasValue)
                {
                    filter &= builder.Lte(o => o.CreatedAt, endTime.Value);
                }

                var total = await _orders.CountDocumentsAsync(filter);
                var orders = await _orders.Find(filter)
                    .SortByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                return (orders, (int)total);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order history: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Order>> GetOpenBuyOrdersAsync(string symbol)
        {
            try
            {
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Symbol, symbol),
                    Builders<Order>.Filter.Eq(o => o.Side, "BUY"),
                    Builders<Order>.Filter.In(o => o.Status, new[] { "NEW", "PARTIALLY_FILLED" }),
                    Builders<Order>.Filter.Eq(o => o.IsWorking, true)
                );

                return await _orders.Find(filter)
                    .SortByDescending(o => o.Price) // Higher buy prices first
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open buy orders: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Order>> GetOpenSellOrdersAsync(string symbol)
        {
            try
            {
                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Symbol, symbol),
                    Builders<Order>.Filter.Eq(o => o.Side, "SELL"),
                    Builders<Order>.Filter.In(o => o.Status, new[] { "NEW", "PARTIALLY_FILLED" }),
                    Builders<Order>.Filter.Eq(o => o.IsWorking, true)
                );

                return await _orders.Find(filter)
                    .SortBy(o => o.Price) // Lower sell prices first
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open sell orders: {ex.Message}");
                throw;
            }
        }
    }
}