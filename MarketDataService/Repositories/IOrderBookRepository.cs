using System.Threading.Tasks;
using CommonLib.Models.Market;
using MongoDB.Bson;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for OrderBook entity
    /// </summary>
    public interface IOrderBookRepository
    {
        /// <summary>
        /// Get order book for a specific symbol
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <returns>Order book if found, null otherwise</returns>
        Task<OrderBook> GetOrderBookBySymbolAsync(string symbolName);

        /// <summary>
        /// Get order book by ID
        /// </summary>
        /// <param name="id">Order book ID</param>
        /// <returns>Order book if found, null otherwise</returns>
        Task<OrderBook> GetOrderBookByIdAsync(ObjectId id);

        /// <summary>
        /// Create or update order book
        /// </summary>
        /// <param name="orderBook">Order book to update</param>
        /// <returns>Updated order book</returns>
        Task<OrderBook> UpsertOrderBookAsync(OrderBook orderBook);
    }
}