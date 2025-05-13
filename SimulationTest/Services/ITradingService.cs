using CommonLib.Models;
using CommonLib.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for trading operations
    /// </summary>
    public interface ITradingService
    {
        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <returns>Created order details</returns>
        Task<Order> CreateOrderAsync(CreateOrderRequest request);

        /// <summary>
        /// Cancels an existing order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Success or failure message</returns>
        Task<string> CancelOrderAsync(string orderId);

        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Order details</returns>
        Task<Order> GetOrderAsync(string orderId);

        /// <summary>
        /// Gets open orders for the current user
        /// </summary>
        /// <param name="symbol">Optional symbol filter</param>
        /// <returns>List of open orders</returns>
        Task<List<Order>> GetOpenOrdersAsync(string symbol = null);

        /// <summary>
        /// Gets order history for the current user
        /// </summary>
        /// <param name="request">Order history request parameters</param>
        /// <returns>Order history with pagination</returns>
        Task<PaginatedResult<Order>> GetOrderHistoryAsync(OrderHistoryRequest request);

        /// <summary>
        /// Gets trade history for the current user
        /// </summary>
        /// <param name="request">Trade history request parameters</param>
        /// <returns>Trade history with pagination</returns>
        Task<PaginatedResult<Trade>> GetTradeHistoryAsync(TradeHistoryRequest request);
    }
}