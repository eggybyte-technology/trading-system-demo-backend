using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MatchMakingService.Repositories
{
    /// <summary>
    /// Repository for OrderMatcher operations
    /// </summary>
    public class OrderMatcherRepository
    {
        private readonly IMongoCollection<OrderMatcher> _collection;

        /// <summary>
        /// Initializes a new instance of the OrderMatcherRepository class
        /// </summary>
        public OrderMatcherRepository(MongoDbConnectionFactory dbFactory)
        {
            _collection = dbFactory.GetCollection<OrderMatcher>();
        }

        /// <summary>
        /// Gets all active order matchers
        /// </summary>
        public async Task<List<OrderMatcher>> GetActiveMatchersAsync(CancellationToken cancellationToken = default)
        {
            var filter = Builders<OrderMatcher>.Filter.Eq(m => m.IsActive, true);
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Updates an order matcher
        /// </summary>
        public async Task UpdateMatcherAsync(OrderMatcher matcher, CancellationToken cancellationToken = default)
        {
            var filter = Builders<OrderMatcher>.Filter.Eq(m => m.Id, matcher.Id);
            await _collection.ReplaceOneAsync(filter, matcher, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new order matcher
        /// </summary>
        public async Task CreateMatcherAsync(OrderMatcher matcher, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(matcher, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets a matcher by symbol
        /// </summary>
        public async Task<OrderMatcher> GetMatcherBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var filter = Builders<OrderMatcher>.Filter.Eq(m => m.Symbol, symbol);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
    }
}