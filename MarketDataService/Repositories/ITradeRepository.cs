using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using MongoDB.Bson;
using CommonLib.Models.Market;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for trade data
    /// </summary>
    public interface ITradeRepository
    {
        /// <summary>
        /// Gets trades for a symbol within a time range
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="startTime">Range start time</param>
        /// <param name="endTime">Range end time</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of trades</returns>
        Task<List<Trade>> GetTradesInTimeRangeAsync(string symbol, DateTime startTime, DateTime endTime, int limit = 1000);

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of trades</returns>
        Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit = 100);

        /// <summary>
        /// Saves a trade
        /// </summary>
        /// <param name="trade">The trade to save</param>
        /// <returns>The saved trade</returns>
        Task<Trade> SaveTradeAsync(Trade trade);

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