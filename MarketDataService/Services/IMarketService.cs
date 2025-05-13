using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Models.Trading;

namespace MarketDataService.Services
{
    /// <summary>
    /// Service interface for market data operations
    /// </summary>
    public interface IMarketService
    {
        /// <summary>
        /// Gets all symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        Task<List<Symbol>> GetSymbolsAsync();

        /// <summary>
        /// Gets ticker data for a symbol
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <returns>Market data for the symbol</returns>
        Task<MarketData> GetTickerAsync(string symbolName);

        /// <summary>
        /// Gets market summary for all symbols
        /// </summary>
        /// <returns>List of market data for all symbols</returns>
        Task<List<MarketData>> GetMarketSummaryAsync();

        /// <summary>
        /// Gets market depth (order book) for a symbol
        /// </summary>
        /// <param name="request">Market depth request</param>
        /// <returns>Order book for the symbol</returns>
        Task<OrderBook> GetOrderBookDepthAsync(MarketDepthRequest request);

        /// <summary>
        /// Gets klines (candlestick data) for a symbol
        /// </summary>
        /// <param name="request">Kline request</param>
        /// <returns>List of klines</returns>
        Task<List<Kline>> GetKlinesAsync(KlineRequest request);

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="request">Recent trades request</param>
        /// <returns>List of recent trades</returns>
        Task<List<Trade>> GetRecentTradesAsync(RecentTradesRequest request);

        /// <summary>
        /// Creates a new trading symbol
        /// </summary>
        /// <param name="request">Symbol creation request</param>
        /// <returns>Created symbol</returns>
        Task<Symbol> CreateSymbolAsync(SymbolCreateRequest request);

        /// <summary>
        /// Updates an existing trading symbol
        /// </summary>
        /// <param name="symbolName">Symbol name to update</param>
        /// <param name="request">Symbol update request</param>
        /// <returns>Updated symbol</returns>
        Task<Symbol> UpdateSymbolAsync(string symbolName, SymbolUpdateRequest request);
    }
}