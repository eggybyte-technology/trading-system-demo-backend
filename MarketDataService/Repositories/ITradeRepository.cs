using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using MongoDB.Bson;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for Trade entity
    /// </summary>
    public interface ITradeRepository
    {
        /// <summary>
        /// Get recent trades for a specific symbol
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of recent trades</returns>
        Task<List<Trade>> GetRecentTradesAsync(string symbolName, int limit = 100);

        /// <summary>
        /// Get trades for a specific symbol within a time range
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <param name="startTime">Optional start time</param>
        /// <param name="endTime">Optional end time</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of trades</returns>
        Task<List<Trade>> GetTradesInTimeRangeAsync(string symbolName, DateTime? startTime = null, DateTime? endTime = null, int limit = 500);

        /// <summary>
        /// Get trade by ID
        /// </summary>
        /// <param name="id">Trade ID</param>
        /// <returns>Trade if found, null otherwise</returns>
        Task<Trade> GetTradeByIdAsync(ObjectId id);

        /// <summary>
        /// Add new trade
        /// </summary>
        /// <param name="trade">Trade to add</param>
        /// <returns>Added trade</returns>
        Task<Trade> AddTradeAsync(Trade trade);

        /// <summary>
        /// Add multiple trades in batch
        /// </summary>
        /// <param name="trades">List of trades to add</param>
        /// <returns>Number of trades added</returns>
        Task<int> AddTradesAsync(List<Trade> trades);
    }
}