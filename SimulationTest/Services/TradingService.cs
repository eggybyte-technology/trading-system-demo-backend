using CommonLib.Models;
using CommonLib.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Implementation of the trading service for order and trade operations
    /// </summary>
    public class TradingService : ITradingService
    {
        private readonly IHttpClientService _httpClient;

        /// <summary>
        /// Initializes a new instance of the TradingService
        /// </summary>
        /// <param name="httpClient">HTTP client service</param>
        public TradingService(IHttpClientService httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <returns>Created order details</returns>
        public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
        {
            return await _httpClient.PostAsync<CreateOrderRequest, Order>("trading", "order", request);
        }

        /// <summary>
        /// Cancels an existing order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Success or failure message</returns>
        public async Task<string> CancelOrderAsync(string orderId)
        {
            // The API returns a status message for order cancellation
            var response = await _httpClient.DeleteAsync<dynamic>("trading", $"order/{orderId}");
            return response?.message?.ToString() ?? "Order canceled successfully";
        }

        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Order details</returns>
        public async Task<Order> GetOrderAsync(string orderId)
        {
            return await _httpClient.GetAsync<Order>("trading", $"order/{orderId}");
        }

        /// <summary>
        /// Gets open orders for the current user
        /// </summary>
        /// <param name="symbol">Optional symbol filter</param>
        /// <returns>List of open orders</returns>
        public async Task<List<Order>> GetOpenOrdersAsync(string symbol = null)
        {
            var queryParams = string.IsNullOrEmpty(symbol) ? null : $"symbol={symbol}";
            return await _httpClient.GetAsync<List<Order>>("trading", "order/open", queryParams);
        }

        /// <summary>
        /// Gets order history for the current user
        /// </summary>
        /// <param name="request">Order history request parameters</param>
        /// <returns>Order history with pagination</returns>
        public async Task<PaginatedResult<Order>> GetOrderHistoryAsync(OrderHistoryRequest request)
        {
            var queryParams = $"page={request.Page}&pageSize={request.PageSize}";

            if (!string.IsNullOrEmpty(request.Symbol))
                queryParams += $"&symbol={request.Symbol}";

            if (request.StartTime.HasValue)
                queryParams += $"&startTime={request.StartTime.Value}";

            if (request.EndTime.HasValue)
                queryParams += $"&endTime={request.EndTime.Value}";

            return await _httpClient.GetAsync<PaginatedResult<Order>>("trading", "order/history", queryParams);
        }

        /// <summary>
        /// Gets trade history for the current user
        /// </summary>
        /// <param name="request">Trade history request parameters</param>
        /// <returns>Trade history with pagination</returns>
        public async Task<PaginatedResult<Trade>> GetTradeHistoryAsync(TradeHistoryRequest request)
        {
            var queryParams = $"page={request.Page}&pageSize={request.PageSize}";

            if (!string.IsNullOrEmpty(request.Symbol))
                queryParams += $"&symbol={request.Symbol}";

            if (request.StartTime.HasValue)
                queryParams += $"&startTime={request.StartTime.Value}";

            if (request.EndTime.HasValue)
                queryParams += $"&endTime={request.EndTime.Value}";

            return await _httpClient.GetAsync<PaginatedResult<Trade>>("trading", "trade/history", queryParams);
        }
    }
}