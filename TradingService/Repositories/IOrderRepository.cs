using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using MongoDB.Bson;

namespace TradingService.Repositories
{
    /// <summary>
    /// Interface for order repository operations
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <returns>The order or null if not found</returns>
        Task<Order?> GetByIdAsync(ObjectId id);

        /// <summary>
        /// Gets an order by its client order ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The order or null if not found</returns>
        Task<Order?> GetByClientOrderIdAsync(ObjectId userId);

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="order">The order to create</param>
        /// <returns>The created order with ID</returns>
        Task<Order> CreateAsync(Order order);

        /// <summary>
        /// Updates an existing order
        /// </summary>
        /// <param name="order">The order to update</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateAsync(Order order);

        /// <summary>
        /// Deletes/cancels an order
        /// </summary>
        /// <param name="id">The order ID</param>
        /// <returns>True if canceled successfully</returns>
        Task<bool> CancelAsync(ObjectId id);

        /// <summary>
        /// Gets open orders for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="symbol">Optional symbol filter</param>
        /// <returns>List of open orders</returns>
        Task<List<Order>> GetOpenOrdersAsync(ObjectId userId, string? symbol = null);

        /// <summary>
        /// Gets order history for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="symbol">Optional symbol filter</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="startTime">Optional start time filter</param>
        /// <param name="endTime">Optional end time filter</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Order history with pagination</returns>
        Task<(List<Order> Orders, int Total)> GetOrderHistoryAsync(
            ObjectId userId,
            string? symbol = null,
            string? status = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Gets all open buy orders for a symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>List of open buy orders</returns>
        Task<List<Order>> GetOpenBuyOrdersAsync(string symbol);

        /// <summary>
        /// Gets all open sell orders for a symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>List of open sell orders</returns>
        Task<List<Order>> GetOpenSellOrdersAsync(string symbol);
    }
}