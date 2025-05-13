using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommonLib.Models;
using CommonLib.Models.Trading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    public class TradingService : BaseService
    {
        public TradingService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "TradingService", "http://trading.trading-system.local", logger)
        {
        }

        public async Task<OrderResponse> CreateOrderAsync(string token, CreateOrderRequest request)
        {
            return await PostAsync<OrderResponse, CreateOrderRequest>("/order", request, token);
        }

        public async Task<CancelOrderResponse> CancelOrderAsync(string token, string orderId)
        {
            return await DeleteAsync<CancelOrderResponse>($"/order/{orderId}", token);
        }

        public async Task<OrderResponse> GetOrderDetailsAsync(string token, string orderId)
        {
            return await GetAsync<OrderResponse>($"/order/{orderId}", token);
        }

        public async Task<OpenOrdersResponse> GetOpenOrdersAsync(string token, string? symbol = null)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["symbol"] = symbol
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<OpenOrdersResponse>($"/order/open?{queryString}", token);
        }

        public async Task<OrderHistoryResponse> GetOrderHistoryAsync(string token, OrderHistoryRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = request.Page.ToString(),
                ["pageSize"] = request.PageSize.ToString(),
                ["symbol"] = request.Symbol,
                ["startTime"] = request.StartTime?.ToString(),
                ["endTime"] = request.EndTime?.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<OrderHistoryResponse>($"/order/history?{queryString}", token);
        }

        public async Task<TradeHistoryResponse> GetTradeHistoryAsync(string token, TradeHistoryRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = request.Page.ToString(),
                ["pageSize"] = request.PageSize.ToString(),
                ["symbol"] = request.Symbol,
                ["startTime"] = request.StartTime?.ToString(),
                ["endTime"] = request.EndTime?.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<TradeHistoryResponse>($"/trade/history?{queryString}", token);
        }

        /// <summary>
        /// Lock an order for processing in the matching engine
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Order lock request</param>
        /// <returns>Lock order response with status</returns>
        public async Task<LockOrderResponse> LockOrderAsync(string token, LockOrderRequest request)
        {
            return await PostAsync<LockOrderResponse, LockOrderRequest>("/order/lock", request, token);
        }

        /// <summary>
        /// Unlock a previously locked order
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Order unlock request</param>
        /// <returns>Unlock order response with status</returns>
        public async Task<UnlockOrderResponse> UnlockOrderAsync(string token, UnlockOrderRequest request)
        {
            return await PostAsync<UnlockOrderResponse, UnlockOrderRequest>("/order/unlock", request, token);
        }

        /// <summary>
        /// Update the status of an order after processing
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Order status update request</param>
        /// <returns>Updated order information</returns>
        public async Task<OrderResponse> UpdateOrderStatusAsync(string token, UpdateOrderStatusRequest request)
        {
            return await PutAsync<OrderResponse, UpdateOrderStatusRequest>($"/order/status", request, token);
        }
    }
}