using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommonLib.Models.Market;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    public class MarketDataService : BaseService
    {
        public MarketDataService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "MarketDataService", "http://market-data.trading-system.local", logger)
        {
        }

        #region Market Data Queries

        /// <summary>
        /// Gets all available trading symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        public async Task<SymbolsResponse> GetSymbolsAsync()
        {
            return await GetAsync<SymbolsResponse>("/market/symbols");
        }

        /// <summary>
        /// Gets ticker data for a specific symbol
        /// </summary>
        /// <param name="symbol">The symbol name (e.g., BTC-USDT)</param>
        /// <returns>Ticker data for the symbol</returns>
        public async Task<TickerResponse> GetTickerAsync(string symbol)
        {
            return await GetAsync<TickerResponse>($"/market/ticker?symbol={Uri.EscapeDataString(symbol)}");
        }

        /// <summary>
        /// Gets market summary for all trading pairs
        /// </summary>
        /// <returns>Market summary with all tickers</returns>
        public async Task<MarketSummaryResponse> GetMarketSummaryAsync()
        {
            return await GetAsync<MarketSummaryResponse>("/market/summary");
        }

        /// <summary>
        /// Gets the order book (market depth) for a symbol
        /// </summary>
        /// <param name="request">Market depth request with symbol and limit</param>
        /// <returns>Order book data</returns>
        public async Task<MarketDepthResponse> GetOrderBookDepthAsync(MarketDepthRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["symbol"] = request.Symbol,
                ["limit"] = request.Limit.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<MarketDepthResponse>($"/market/depth?{queryString}");
        }

        /// <summary>
        /// Gets kline (candlestick) data for a symbol
        /// </summary>
        /// <param name="request">Kline request with symbol, interval, and time range</param>
        /// <returns>Kline data</returns>
        public async Task<KlineResponse> GetKlinesAsync(KlineRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["symbol"] = request.Symbol,
                ["interval"] = request.Interval,
                ["limit"] = request.Limit.ToString(),
                ["startTime"] = request.StartTime?.ToString(),
                ["endTime"] = request.EndTime?.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<KlineResponse>($"/market/klines?{queryString}");
        }

        /// <summary>
        /// Gets recent trades for a symbol
        /// </summary>
        /// <param name="request">Recent trades request with symbol and limit</param>
        /// <returns>Recent trades data</returns>
        public async Task<TradesResponse> GetRecentTradesAsync(RecentTradesRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["symbol"] = request.Symbol,
                ["limit"] = request.Limit.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<TradesResponse>($"/market/trades?{queryString}");
        }

        #endregion

        #region OrderBook Updates

        /// <summary>
        /// Updates the order book with new price levels
        /// </summary>
        /// <param name="token">Authentication token for service-to-service communication</param>
        /// <param name="request">The order book update request</param>
        /// <returns>Order book update response</returns>
        public async Task<OrderBookUpdateResponse> UpdateOrderBookAsync(string token, OrderBookUpdateRequest request)
        {
            return await PostAsync<OrderBookUpdateResponse, OrderBookUpdateRequest>("/orderbook/update", request, token);
        }

        /// <summary>
        /// Updates the order book directly from WebSocketDepthData
        /// </summary>
        /// <param name="token">Authentication token for service-to-service communication</param>
        /// <param name="depthData">The WebSocket depth data</param>
        /// <returns>Order book update response</returns>
        public async Task<OrderBookUpdateResponse> UpdateOrderBookAsync(string token, WebSocketDepthData depthData)
        {
            var request = new OrderBookUpdateRequest
            {
                Symbol = depthData.Symbol,
                Bids = depthData.Bids,
                Asks = depthData.Asks
            };

            return await UpdateOrderBookAsync(token, request);
        }

        #endregion

        #region Symbol Management

        /// <summary>
        /// Creates a new trading symbol
        /// </summary>
        /// <param name="token">Authentication token for admin operations</param>
        /// <param name="request">Symbol creation request</param>
        /// <returns>Symbol creation response</returns>
        public async Task<SymbolResponse> CreateSymbolAsync(string token, SymbolCreateRequest request)
        {
            return await PostAsync<SymbolResponse, SymbolCreateRequest>("/market/symbols", request, token);
        }

        /// <summary>
        /// Updates an existing trading symbol
        /// </summary>
        /// <param name="token">Authentication token for admin operations</param>
        /// <param name="symbolName">Symbol name to update</param>
        /// <param name="request">Symbol update request</param>
        /// <returns>Symbol update response</returns>
        public async Task<SymbolResponse> UpdateSymbolAsync(string token, string symbolName, SymbolUpdateRequest request)
        {
            return await PutAsync<SymbolResponse, SymbolUpdateRequest>($"/market/symbols/{Uri.EscapeDataString(symbolName)}", request, token);
        }

        #endregion

        #region Kline Processing

        /// <summary>
        /// Processes a trade for kline generation
        /// </summary>
        /// <param name="token">Authentication token for service-to-service communication</param>
        /// <param name="request">Trade data for kline processing</param>
        /// <returns>Processing result</returns>
        public async Task<ApiResponse<bool>> ProcessTradeForKlineAsync(string token, TradeForKlineRequest request)
        {
            return await PostAsync<ApiResponse<bool>, TradeForKlineRequest>("/market/process-trade", request, token);
        }

        #endregion
    }
}