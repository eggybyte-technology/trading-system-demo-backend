using CommonLib.Models.Market;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for market data operations
    /// </summary>
    public interface IMarketService
    {
        /// <summary>
        /// Gets all available trading symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        Task<SymbolsResponse> GetSymbolsAsync();

        /// <summary>
        /// Gets ticker information for a specific symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <returns>Ticker information</returns>
        Task<TickerResponse> GetTickerAsync(string symbol);

        /// <summary>
        /// Gets market summary for all symbols
        /// </summary>
        /// <returns>Market summary</returns>
        Task<MarketSummaryResponse> GetMarketSummaryAsync();

        /// <summary>
        /// Gets order book depth for a symbol
        /// </summary>
        /// <param name="request">Market depth request</param>
        /// <returns>Order book depth</returns>
        Task<MarketDepthResponse> GetMarketDepthAsync(MarketDepthRequest request);

        /// <summary>
        /// Gets kline/candlestick data for a symbol
        /// </summary>
        /// <param name="request">Kline request</param>
        /// <returns>Kline data</returns>
        Task<List<decimal[]>> GetKlinesAsync(KlineRequest request);

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of trades</returns>
        Task<List<TradeResponse>> GetRecentTradesAsync(string symbol, int limit = 100);
    }
}