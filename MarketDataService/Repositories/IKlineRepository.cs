using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using MongoDB.Bson;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for Kline entity
    /// </summary>
    public interface IKlineRepository
    {
        /// <summary>
        /// Get klines for a specific symbol and interval with optional time range
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <param name="interval">Kline interval (e.g., 1m, 5m, 1h, 1d)</param>
        /// <param name="startTime">Optional start time</param>
        /// <param name="endTime">Optional end time</param>
        /// <param name="limit">Maximum number of klines to return</param>
        /// <returns>List of klines</returns>
        Task<List<Kline>> GetKlinesAsync(string symbolName, string interval, DateTime? startTime = null, DateTime? endTime = null, int limit = 500);

        /// <summary>
        /// Get kline by ID
        /// </summary>
        /// <param name="id">Kline ID</param>
        /// <returns>Kline if found, null otherwise</returns>
        Task<Kline> GetKlineByIdAsync(ObjectId id);

        /// <summary>
        /// Create a new kline
        /// </summary>
        /// <param name="kline">Kline to create</param>
        /// <returns>Created kline</returns>
        Task<Kline> CreateKlineAsync(Kline kline);

        /// <summary>
        /// Update or create klines in batch
        /// </summary>
        /// <param name="klines">List of klines to upsert</param>
        /// <returns>Number of klines updated/inserted</returns>
        Task<int> UpsertKlinesAsync(List<Kline> klines);
    }
}