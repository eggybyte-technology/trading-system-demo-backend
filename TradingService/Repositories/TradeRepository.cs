using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using CommonLib.Models.Trading;

namespace TradingService.Repositories
{
    /// <summary>
    /// Repository for trade operations
    /// </summary>
    public class TradeRepository : ITradeRepository
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly IMongoCollection<Trade> _trades;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the TradeRepository
        /// </summary>
        /// <param name="dbFactory">The MongoDB connection factory</param>
        /// <param name="logger">The logger service</param>
        public TradeRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trades = _dbFactory.GetCollection<Trade>();
        }

        /// <summary>
        /// Gets a trade by its ID
        /// </summary>
        /// <param name="id">The trade ID</param>
        /// <returns>The trade or null if not found</returns>
        public async Task<Trade?> GetByIdAsync(ObjectId id)
        {
            try
            {
                return await _trades.Find(t => t.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trade by ID: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new trade
        /// </summary>
        /// <param name="trade">The trade to create</param>
        /// <returns>The created trade with ID</returns>
        public async Task<Trade> CreateAsync(Trade trade)
        {
            try
            {
                // Set timestamps if not already set
                if (trade.CreatedAt == default)
                {
                    trade.CreatedAt = DateTime.UtcNow;
                }

                await _trades.InsertOneAsync(trade);
                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating trade: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets trades for a specific order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>List of trades</returns>
        public async Task<List<Trade>> GetTradesByOrderIdAsync(ObjectId orderId)
        {
            try
            {
                return await _trades.Find(t => t.OrderId == orderId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trades by order ID: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets trade history for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="symbol">Optional symbol filter</param>
        /// <param name="startTime">Optional start time filter</param>
        /// <param name="endTime">Optional end time filter</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Trade history with pagination</returns>
        public async Task<(List<Trade> Trades, int Total)> GetTradeHistoryAsync(
            ObjectId userId,
            string? symbol = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var filterBuilder = Builders<Trade>.Filter;
                var filter = filterBuilder.Or(
                    filterBuilder.Eq(t => t.BuyerUserId, userId),
                    filterBuilder.Eq(t => t.SellerUserId, userId)
                );

                if (!string.IsNullOrEmpty(symbol))
                {
                    filter &= filterBuilder.Eq(t => t.Symbol, symbol);
                }

                if (startTime.HasValue)
                {
                    filter &= filterBuilder.Gte(t => t.CreatedAt, startTime.Value);
                }

                if (endTime.HasValue)
                {
                    filter &= filterBuilder.Lte(t => t.CreatedAt, endTime.Value);
                }

                var totalCount = await _trades.CountDocumentsAsync(filter);
                var trades = await _trades.Find(filter)
                    .Sort(Builders<Trade>.Sort.Descending(t => t.CreatedAt))
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                return (trades, (int)totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trade history: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of recent trades</returns>
        public async Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit = 20)
        {
            try
            {
                return await _trades.Find(t => t.Symbol == symbol)
                    .Sort(Builders<Trade>.Sort.Descending(t => t.CreatedAt))
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent trades: {ex.Message}");
                throw;
            }
        }
    }
}