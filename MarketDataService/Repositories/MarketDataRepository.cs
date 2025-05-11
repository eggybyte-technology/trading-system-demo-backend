using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// MongoDB implementation of IMarketDataRepository
    /// </summary>
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly IMongoCollection<MarketData> _marketData;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Constructor for MarketDataRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public MarketDataRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _marketData = dbFactory?.GetCollection<MarketData>() ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        /// <inheritdoc />
        public async Task<List<MarketData>> GetAllMarketDataAsync()
        {
            try
            {
                return await _marketData.Find(data => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all market data: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MarketData> GetMarketDataBySymbolAsync(string symbolName)
        {
            try
            {
                return await _marketData.Find(data => data.Symbol == symbolName).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting market data for symbol {symbolName}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MarketData> GetMarketDataByIdAsync(ObjectId id)
        {
            try
            {
                return await _marketData.Find(data => data.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting market data by ID {id}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MarketData> UpsertMarketDataAsync(MarketData marketData)
        {
            try
            {
                var existingData = await GetMarketDataBySymbolAsync(marketData.Symbol);

                if (existingData == null)
                {
                    _logger.LogInformation($"Creating new market data for symbol {marketData.Symbol}");
                    await _marketData.InsertOneAsync(marketData);
                    return marketData;
                }
                else
                {
                    _logger.LogInformation($"Updating market data for symbol {marketData.Symbol}");
                    marketData.Id = existingData.Id;
                    await _marketData.ReplaceOneAsync(data => data.Id == existingData.Id, marketData);
                    return marketData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error upserting market data for symbol {marketData.Symbol}: {ex.Message}");
                throw;
            }
        }
    }
}