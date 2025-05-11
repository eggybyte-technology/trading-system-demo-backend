using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using CommonLib.Models.Trading;

namespace TradingService.Repositories
{
    /// <summary>
    /// Interface for trade repository operations
    /// </summary>
    public interface ITradeRepository
    {
        /// <summary>
        /// Gets a trade by its ID
        /// </summary>
        /// <param name="id">The trade ID</param>
        /// <returns>The trade or null if not found</returns>
        Task<Trade?> GetByIdAsync(ObjectId id);

        /// <summary>
        /// Creates a new trade
        /// </summary>
        /// <param name="trade">The trade to create</param>
        /// <returns>The created trade with ID</returns>
        Task<Trade> CreateAsync(Trade trade);

        /// <summary>
        /// Gets trades for a specific order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>List of trades</returns>
        Task<List<Trade>> GetTradesByOrderIdAsync(ObjectId orderId);

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
        Task<(List<Trade> Trades, int Total)> GetTradeHistoryAsync(
            ObjectId userId,
            string? symbol = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of recent trades</returns>
        Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit = 20);
    }
}