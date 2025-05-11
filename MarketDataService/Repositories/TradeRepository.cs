using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// MongoDB implementation of ITradeRepository
    /// </summary>
    public class TradeRepository : ITradeRepository
    {
        private readonly IMongoCollection<Trade> _trades;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Constructor for TradeRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public TradeRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _logger = logger;
            _trades = dbFactory.GetCollection<Trade>();

            try
            {
                // Create index on Symbol and CreatedAt
                var indexKeysDefinition = Builders<Trade>.IndexKeys
                    .Ascending(t => t.Symbol)
                    .Descending(t => t.CreatedAt);

                var indexModel = new CreateIndexModel<Trade>(indexKeysDefinition);
                _trades.Indexes.CreateOne(indexModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating index on trades collection: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<List<Trade>> GetRecentTradesAsync(string symbolName, int limit = 100)
        {
            return await _trades.Find(t => t.Symbol == symbolName)
                .Sort(Builders<Trade>.Sort.Descending(t => t.CreatedAt))
                .Limit(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Trade>> GetTradesInTimeRangeAsync(string symbolName, DateTime? startTime = null, DateTime? endTime = null, int limit = 500)
        {
            var filterBuilder = Builders<Trade>.Filter;
            var filter = filterBuilder.Eq(t => t.Symbol, symbolName);

            if (startTime.HasValue)
            {
                filter = filter & filterBuilder.Gte(t => t.CreatedAt, startTime.Value);
            }

            if (endTime.HasValue)
            {
                filter = filter & filterBuilder.Lte(t => t.CreatedAt, endTime.Value);
            }

            return await _trades.Find(filter)
                .Sort(Builders<Trade>.Sort.Ascending(t => t.CreatedAt))
                .Limit(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Trade> GetTradeByIdAsync(ObjectId id)
        {
            return await _trades.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<Trade> AddTradeAsync(Trade trade)
        {
            await _trades.InsertOneAsync(trade);
            return trade;
        }

        /// <inheritdoc />
        public async Task<int> AddTradesAsync(List<Trade> trades)
        {
            if (trades == null || trades.Count == 0)
            {
                return 0;
            }

            await _trades.InsertManyAsync(trades);
            return trades.Count;
        }
    }
}