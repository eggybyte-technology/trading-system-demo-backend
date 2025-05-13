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
        public async Task<MarketData> InitMarketDataAsync(string symbolName, string baseAsset, string quoteAsset)
        {
            try
            {
                var existingData = await GetMarketDataBySymbolAsync(symbolName);
                if (existingData != null)
                {
                    return existingData;
                }

                var marketData = new MarketData
                {
                    Symbol = symbolName,
                    LastPrice = 0,
                    PriceChange = 0,
                    PriceChangePercent = 0,
                    High24h = 0,
                    Low24h = 0,
                    Volume24h = 0,
                    QuoteVolume24h = 0,
                    UpdatedAt = DateTime.UtcNow
                };

                await _marketData.InsertOneAsync(marketData);
                return marketData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing market data for symbol {symbolName}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MarketData> UpdateMarketDataWithTradeAsync(string symbolName, decimal price, decimal quantity, bool isBuyerMaker)
        {
            try
            {
                var marketData = await GetMarketDataBySymbolAsync(symbolName);
                if (marketData == null)
                {
                    throw new KeyNotFoundException($"Market data for symbol {symbolName} not found");
                }

                // If this is the first trade, set the initial prices
                if (marketData.LastPrice == 0)
                {
                    marketData.High24h = price;
                    marketData.Low24h = price;
                }

                // Update 24h high/low
                marketData.High24h = Math.Max(marketData.High24h, price);
                marketData.Low24h = marketData.Low24h == 0 ? price : Math.Min(marketData.Low24h, price);

                // Calculate price change (based on the first recorded price of the period)
                if (marketData.PriceChange == 0)
                {
                    // First trade of the period, no change yet
                    marketData.PriceChange = 0;
                    marketData.PriceChangePercent = 0;
                }
                else
                {
                    // Calculate change from the first price of the period to current price
                    decimal firstPrice = marketData.LastPrice - marketData.PriceChange;
                    marketData.PriceChange = price - firstPrice;
                    marketData.PriceChangePercent = firstPrice == 0 ? 0 :
                        ((price - firstPrice) / firstPrice) * 100;
                }

                // Update volumes
                marketData.Volume24h += quantity;
                marketData.QuoteVolume24h += quantity * price;

                // Update last price
                marketData.LastPrice = price;
                marketData.UpdatedAt = DateTime.UtcNow;

                await UpdateMarketDataAsync(marketData);
                return marketData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating market data with trade for symbol {symbolName}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MarketData> UpdateMarketDataAsync(MarketData marketData)
        {
            try
            {
                marketData.UpdatedAt = DateTime.UtcNow;
                await _marketData.ReplaceOneAsync(data => data.Id == marketData.Id, marketData);
                return marketData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating market data for symbol {marketData.Symbol}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteMarketDataAsync(string symbolName)
        {
            try
            {
                var result = await _marketData.DeleteOneAsync(data => data.Symbol == symbolName);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting market data for symbol {symbolName}: {ex.Message}");
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