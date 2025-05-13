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
        /// Get all market data
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
        /// Initialize market data for a symbol
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <param name="baseAsset">Base asset (e.g., BTC)</param>
        /// <param name="quoteAsset">Quote asset (e.g., USDT)</param>
        /// <returns>Initialized market data</returns>
        Task<MarketData> InitMarketDataAsync(string symbolName, string baseAsset, string quoteAsset);

        /// <summary>
        /// Update market data with a new trade
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <param name="price">Trade price</param>
        /// <param name="quantity">Trade quantity</param>
        /// <param name="isBuyerMaker">Whether the buyer is the maker</param>
        /// <returns>Updated market data</returns>
        Task<MarketData> UpdateMarketDataWithTradeAsync(string symbolName, decimal price, decimal quantity, bool isBuyerMaker);

        /// <summary>
        /// Update ticker data
        /// </summary>
        /// <param name="marketData">Market data to update</param>
        /// <returns>Updated market data</returns>
        Task<MarketData> UpdateMarketDataAsync(MarketData marketData);

        /// <summary>
        /// Delete market data for a symbol
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteMarketDataAsync(string symbolName);
    }
}