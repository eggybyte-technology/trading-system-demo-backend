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
        /// Get order book for a symbol
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <returns>Order book for the symbol</returns>
        Task<OrderBook> GetOrderBookBySymbolAsync(string symbolName);

        /// <summary>
        /// Gets order book by its ID
        /// </summary>
        /// <param name="id">Order book ID</param>
        /// <returns>Order book if found, null otherwise</returns>
        Task<OrderBook> GetOrderBookByIdAsync(ObjectId id);

        /// <summary>
        /// Initialize order book for a symbol
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <returns>Initialized order book</returns>
        Task<OrderBook> InitOrderBookAsync(string symbolName);

        /// <summary>
        /// Update order book with new price levels
        /// </summary>
        /// <param name="orderBook">Updated order book</param>
        /// <returns>Updated order book</returns>
        Task<OrderBook> UpdateOrderBookAsync(OrderBook orderBook);

        /// <summary>
        /// Add or update order book (inserts if doesn't exist, updates if exists)
        /// </summary>
        /// <param name="orderBook">Order book to insert or update</param>
        /// <returns>Upserted order book</returns>
        Task<OrderBook> UpsertOrderBookAsync(OrderBook orderBook);

        /// <summary>
        /// Add or update a single price level in the order book
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <param name="priceLevel">Price level to add or update</param>
        /// <param name="isBid">Whether the price level is a bid</param>
        /// <returns>True if successful</returns>
        Task<bool> AddOrUpdatePriceLevelAsync(string symbolName, PriceLevel priceLevel, bool isBid);

        /// <summary>
        /// Remove a single price level from the order book
        /// </summary>
        /// <param name="symbolName">Symbol name</param>
        /// <param name="price">Price to remove</param>
        /// <param name="isBid">Whether the price is a bid</param>
        /// <returns>True if successful</returns>
        Task<bool> RemovePriceLevelAsync(string symbolName, decimal price, bool isBid);
    }
}