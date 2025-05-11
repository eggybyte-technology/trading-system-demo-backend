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
    }
}