using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Models.Trading;

namespace MarketDataService.Services
{
    /// <summary>
    /// Service interface for market operations
    /// </summary>
    public interface IMarketService
    {
        /// <summary>
        /// Get all available trading symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        Task<List<Symbol>> GetSymbolsAsync();

        /// <summary>
        /// Get ticker information for a specific symbol
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <returns>Ticker information</returns>
        Task<MarketData> GetTickerAsync(string symbolName);

        /// <summary>
        /// Get market summary for all symbols
        /// </summary>
        /// <returns>Market summary</returns>
        Task<List<MarketData>> GetMarketSummaryAsync();

        /// <summary>
        /// Get order book depth for a symbol
        /// </summary>
        /// <param name="request">Market depth request parameters</param>
        /// <returns>Order book depth</returns>
        Task<OrderBook> GetMarketDepthAsync(MarketDepthRequest request);

        /// <summary>
        /// Get kline/candlestick data for a symbol
        /// </summary>
        /// <param name="request">Kline request parameters</param>
        /// <returns>Kline data</returns>
        Task<List<Kline>> GetKlinesAsync(KlineRequest request);

        /// <summary>
        /// Get recent trades for a symbol
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of trades</returns>
        Task<List<Trade>> GetRecentTradesAsync(string symbolName, int limit = 100);
    }
}