using CommonLib.Models.Market;
using CommonLib.Models.Trading;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace MarketDataService.Services
{
    /// <summary>
    /// Service interface for Kline generation and management
    /// </summary>
    public interface IKlineService
    {
        /// <summary>
        /// Updates Kline data with a new trade
        /// </summary>
        /// <param name="trade">The trade to process</param>
        /// <returns>Async task</returns>
        Task UpdateKlineWithTradeAsync(Trade trade);

        /// <summary>
        /// Manually generates Kline data for a specific symbol and interval
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="interval">Kline interval (e.g., "1m", "5m", "1h")</param>
        /// <param name="startTime">Start time for kline generation</param>
        /// <param name="endTime">End time for kline generation</param>
        /// <returns>Async task</returns>
        Task GenerateKlineAsync(string symbol, string interval, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Runs the kline aggregation task for the specified interval
        /// </summary>
        /// <param name="interval">The interval to aggregate (e.g., "1m", "5m", "1h")</param>
        /// <returns>Async task</returns>
        Task RunKlineAggregationTaskAsync(string interval);

        /// <summary>
        /// Calculates the time range for a kline interval
        /// </summary>
        /// <param name="interval">Kline interval (e.g., "1m", "5m", "1h")</param>
        /// <param name="timestamp">Reference timestamp</param>
        /// <returns>Tuple with start and end time</returns>
        (DateTime startTime, DateTime endTime) CalculateKlineTimeRange(string interval, DateTime timestamp);

        /// <summary>
        /// Converts a Kline model to KlineData for WebSocket
        /// </summary>
        /// <param name="kline">The kline to convert</param>
        /// <returns>KlineData for WebSocket</returns>
        KlineData KlineToKlineData(Kline kline);
    }
}