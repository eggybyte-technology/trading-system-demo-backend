using System;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// MongoDB implementation of IOrderBookRepository
    /// </summary>
    public class OrderBookRepository : IOrderBookRepository
    {
        private readonly IMongoCollection<OrderBook> _orderBooks;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Constructor for OrderBookRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public OrderBookRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _logger = logger;
            _orderBooks = dbFactory.GetCollection<OrderBook>();

            try
            {
                // Create index on Symbol
                var indexKeysDefinition = Builders<OrderBook>.IndexKeys.Ascending(o => o.Symbol);
                var indexModel = new CreateIndexModel<OrderBook>(indexKeysDefinition, new CreateIndexOptions { Unique = true });
                _orderBooks.Indexes.CreateOne(indexModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating index on order books collection: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<OrderBook> GetOrderBookBySymbolAsync(string symbolName)
        {
            return await _orderBooks.Find(orderBook => orderBook.Symbol == symbolName).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<OrderBook> GetOrderBookByIdAsync(ObjectId id)
        {
            return await _orderBooks.Find(orderBook => orderBook.Id == id).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<OrderBook> InitOrderBookAsync(string symbolName)
        {
            var existingOrderBook = await GetOrderBookBySymbolAsync(symbolName);
            if (existingOrderBook != null)
            {
                return existingOrderBook;
            }

            var orderBook = new OrderBook
            {
                Symbol = symbolName,
                UpdatedAt = DateTime.UtcNow,
                Bids = new List<PriceLevel>(),
                Asks = new List<PriceLevel>()
            };

            await _orderBooks.InsertOneAsync(orderBook);
            return orderBook;
        }

        /// <inheritdoc />
        public async Task<OrderBook> UpdateOrderBookAsync(OrderBook orderBook)
        {
            await _orderBooks.ReplaceOneAsync(ob => ob.Id == orderBook.Id, orderBook);
            return orderBook;
        }

        /// <inheritdoc />
        public async Task<OrderBook> UpsertOrderBookAsync(OrderBook orderBook)
        {
            var existingOrderBook = await GetOrderBookBySymbolAsync(orderBook.Symbol);

            if (existingOrderBook == null)
            {
                await _orderBooks.InsertOneAsync(orderBook);
                return orderBook;
            }
            else
            {
                orderBook.Id = existingOrderBook.Id;
                await _orderBooks.ReplaceOneAsync(ob => ob.Id == existingOrderBook.Id, orderBook);
                return orderBook;
            }
        }

        /// <inheritdoc />
        public async Task<bool> AddOrUpdatePriceLevelAsync(string symbolName, PriceLevel priceLevel, bool isBid)
        {
            var orderBook = await GetOrderBookBySymbolAsync(symbolName);
            if (orderBook == null)
            {
                return false;
            }

            var priceLevels = isBid ? orderBook.Bids : orderBook.Asks;
            var existingLevel = priceLevels.FirstOrDefault(pl => pl.Price == priceLevel.Price);

            if (existingLevel != null)
            {
                existingLevel.Quantity = priceLevel.Quantity;
            }
            else
            {
                priceLevels.Add(priceLevel);
            }

            orderBook.UpdatedAt = DateTime.UtcNow;

            // Sort bids (descending) and asks (ascending)
            if (isBid)
            {
                orderBook.Bids = orderBook.Bids.OrderByDescending(b => b.Price).ToList();
            }
            else
            {
                orderBook.Asks = orderBook.Asks.OrderBy(a => a.Price).ToList();
            }

            await UpdateOrderBookAsync(orderBook);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RemovePriceLevelAsync(string symbolName, decimal price, bool isBid)
        {
            var orderBook = await GetOrderBookBySymbolAsync(symbolName);
            if (orderBook == null)
            {
                return false;
            }

            var priceLevels = isBid ? orderBook.Bids : orderBook.Asks;
            var existingLevel = priceLevels.FirstOrDefault(pl => pl.Price == price);

            if (existingLevel != null)
            {
                priceLevels.Remove(existingLevel);
                orderBook.UpdatedAt = DateTime.UtcNow;
                await UpdateOrderBookAsync(orderBook);
                return true;
            }

            return false;
        }
    }
}