using CommonLib.Models.Market;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Implementation of the market service for market data operations
    /// </summary>
    public class MarketService : IMarketService
    {
        private readonly IHttpClientService _httpClient;

        /// <summary>
        /// Initializes a new instance of the MarketService
        /// </summary>
        /// <param name="httpClient">HTTP client service</param>
        public MarketService(IHttpClientService httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets all available trading symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        public async Task<SymbolsResponse> GetSymbolsAsync()
        {
            return await _httpClient.GetAsync<SymbolsResponse>("market", "market/symbols");
        }

        /// <summary>
        /// Gets ticker information for a specific symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <returns>Ticker information</returns>
        public async Task<TickerResponse> GetTickerAsync(string symbol)
        {
            return await _httpClient.GetAsync<TickerResponse>("market", "market/ticker", $"symbol={symbol}");
        }

        /// <summary>
        /// Gets market summary for all symbols
        /// </summary>
        /// <returns>Market summary</returns>
        public async Task<MarketSummaryResponse> GetMarketSummaryAsync()
        {
            return await _httpClient.GetAsync<MarketSummaryResponse>("market", "market/summary");
        }

        /// <summary>
        /// Gets order book depth for a symbol
        /// </summary>
        /// <param name="request">Market depth request</param>
        /// <returns>Order book depth</returns>
        public async Task<MarketDepthResponse> GetMarketDepthAsync(MarketDepthRequest request)
        {
            var queryParams = $"symbol={request.Symbol}&limit={request.Limit}";
            return await _httpClient.GetAsync<MarketDepthResponse>("market", "market/depth", queryParams);
        }

        /// <summary>
        /// Gets kline/candlestick data for a symbol
        /// </summary>
        /// <param name="request">Kline request</param>
        /// <returns>Kline data</returns>
        public async Task<List<decimal[]>> GetKlinesAsync(KlineRequest request)
        {
            var queryParams = $"symbol={request.Symbol}&interval={request.Interval}&limit={request.Limit}";

            if (request.StartTime.HasValue)
                queryParams += $"&startTime={request.StartTime.Value}";

            if (request.EndTime.HasValue)
                queryParams += $"&endTime={request.EndTime.Value}";

            return await _httpClient.GetAsync<List<decimal[]>>("market", "market/klines", queryParams);
        }

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Maximum number of trades to return</param>
        /// <returns>List of trades</returns>
        public async Task<List<TradeResponse>> GetRecentTradesAsync(string symbol, int limit = 100)
        {
            var queryParams = $"symbol={symbol}&limit={limit}";
            return await _httpClient.GetAsync<List<TradeResponse>>("market", "market/trades", queryParams);
        }
    }
}