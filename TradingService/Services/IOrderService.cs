using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using MongoDB.Bson;

namespace TradingService.Services
{
    /// <summary>
    /// Interface for order management service operations
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Creates a new trading order
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="request">Order creation request</param>
        /// <returns>Created order details</returns>
        Task<Order> CreateOrderAsync(ObjectId userId, CreateOrderRequest request);

        /// <summary>
        /// Cancels an existing order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="userId">The user ID (for ownership verification)</param>
        /// <returns>True if the order was canceled successfully</returns>
        Task<bool> CancelOrderAsync(ObjectId orderId, ObjectId userId);

        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="userId">The user ID (for ownership verification)</param>
        /// <returns>Order details or null if not found</returns>
        Task<Order?> GetOrderAsync(ObjectId orderId, ObjectId userId);

        /// <summary>
        /// Gets all open orders for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="symbol">Optional symbol filter</param>
        /// <returns>List of open orders</returns>
        Task<List<Order>> GetOpenOrdersAsync(ObjectId userId, string? symbol = null);

        /// <summary>
        /// Gets order history for a user with pagination
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="request">History request parameters (symbol, status, timeframe, pagination)</param>
        /// <returns>Order history with pagination</returns>
        Task<(List<Order> Orders, int Total)> GetOrderHistoryAsync(ObjectId userId, OrderHistoryRequest request);

        /// <summary>
        /// Gets trade history for a user with pagination
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="symbol">Optional symbol filter</param>
        /// <param name="startTime">Optional start time filter (Unix timestamp in seconds)</param>
        /// <param name="endTime">Optional end time filter (Unix timestamp in seconds)</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Trade history with pagination</returns>
        Task<(List<Trade> Trades, int Total)> GetTradeHistoryAsync(
            ObjectId userId,
            string? symbol = null,
            long? startTime = null,
            long? endTime = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Locks an order for the matching process
        /// </summary>
        /// <param name="request">Lock order request</param>
        /// <returns>Lock order response</returns>
        Task<LockOrderResponse> LockOrderAsync(LockOrderRequest request);

        /// <summary>
        /// Unlocks a previously locked order
        /// </summary>
        /// <param name="request">Unlock order request</param>
        /// <returns>Unlock order response</returns>
        Task<UnlockOrderResponse> UnlockOrderAsync(UnlockOrderRequest request);

        /// <summary>
        /// Updates order status after matching
        /// </summary>
        /// <param name="request">Update order status request</param>
        /// <returns>Success flag</returns>
        Task<bool> UpdateOrderStatusAsync(UpdateOrderStatusRequest request);
    }
}