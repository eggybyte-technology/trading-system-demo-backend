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

        public async Task<SymbolsResponse> GetSymbolsAsync()
        {
            return await GetAsync<SymbolsResponse>("/market/symbols");
        }

        public async Task<TickerResponse> GetTickerAsync(string symbol)
        {
            return await GetAsync<TickerResponse>($"/market/ticker?symbol={Uri.EscapeDataString(symbol)}");
        }

        public async Task<MarketSummaryResponse> GetMarketSummaryAsync()
        {
            return await GetAsync<MarketSummaryResponse>("/market/summary");
        }

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
    }
}