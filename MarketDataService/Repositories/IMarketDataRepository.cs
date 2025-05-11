using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using MongoDB.Bson;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for MarketData entity
    /// </summary>
    public interface IMarketDataRepository
    {
        /// <summary>
        /// Get market data for all symbols
        /// </summary>
        /// <returns>List of all market data</returns>
        Task<List<MarketData>> GetAllMarketDataAsync();

        /// <summary>
        /// Get market data for a specific symbol
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <returns>Market data if found, null otherwise</returns>
        Task<MarketData> GetMarketDataBySymbolAsync(string symbolName);

        /// <summary>
        /// Get market data by ID
        /// </summary>
        /// <param name="id">Market data ID</param>
        /// <returns>Market data if found, null otherwise</returns>
        Task<MarketData> GetMarketDataByIdAsync(ObjectId id);

        /// <summary>
        /// Create or update market data
        /// </summary>
        /// <param name="marketData">Market data to update</param>
        /// <returns>Updated market data</returns>
        Task<MarketData> UpsertMarketDataAsync(MarketData marketData);
    }
}