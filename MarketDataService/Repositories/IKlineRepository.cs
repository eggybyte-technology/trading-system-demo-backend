using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using MongoDB.Bson;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for Kline data
    /// </summary>
    public interface IKlineRepository
    {
        /// <summary>
        /// Gets a kline by symbol, interval, and start time
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="interval">Kline interval</param>
        /// <param name="startTime">Kline start time</param>
        /// <returns>The kline or null if not found</returns>
        Task<Kline?> GetKlineAsync(string symbol, string interval, DateTime startTime);

        /// <summary>
        /// Gets klines for a symbol and interval within a time range
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="interval">Kline interval</param>
        /// <param name="startTime">Range start time</param>
        /// <param name="endTime">Range end time</param>
        /// <param name="limit">Maximum number of klines to return</param>
        /// <returns>List of klines</returns>
        Task<List<Kline>> GetKlinesAsync(string symbol, string interval, DateTime startTime, DateTime endTime, int limit = 500);

        /// <summary>
        /// Inserts or updates a kline
        /// </summary>
        /// <param name="kline">The kline to upsert</param>
        /// <returns>The upserted kline</returns>
        Task<Kline> UpsertKlineAsync(Kline kline);

        /// <summary>
        /// Deletes klines for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <returns>Number of klines deleted</returns>
        Task<int> DeleteKlinesAsync(string symbol);

        /// <summary>
        /// Gets the latest kline for a symbol and interval
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="interval">Kline interval</param>
        /// <returns>The latest kline or null if not found</returns>
        Task<Kline?> GetLatestKlineAsync(string symbol, string interval);
    }
}