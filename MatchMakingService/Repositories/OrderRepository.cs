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
    /// Repository for order operations needed by the matching engine
    /// </summary>
    public class OrderRepository
    {
        private readonly IMongoCollection<Order> _orderCollection;
        private readonly IMongoCollection<Trade> _tradeCollection;

        /// <summary>
        /// Initializes a new instance of the OrderRepository class
        /// </summary>
        public OrderRepository(MongoDbConnectionFactory dbFactory)
        {
            _orderCollection = dbFactory.GetCollection<Order>();
            _tradeCollection = dbFactory.GetCollection<Trade>();
        }

        /// <summary>
        /// Gets active buy orders for a symbol, sorted by price (descending) and time (ascending)
        /// </summary>
        public async Task<List<Order>> GetActiveBuyOrdersAsync(string symbol, int limit = 1000, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.Eq(o => o.Symbol, symbol),
                Builders<Order>.Filter.Eq(o => o.Side, "BUY"),
                Builders<Order>.Filter.Eq(o => o.Status, "NEW"),
                Builders<Order>.Filter.Eq(o => o.IsWorking, true),
                Builders<Order>.Filter.Eq(o => o.IsLocked, false)
            );

            var sort = Builders<Order>.Sort
                .Descending(o => o.Price)
                .Ascending(o => o.CreatedAt);

            return await _orderCollection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets active sell orders for a symbol, sorted by price (ascending) and time (ascending)
        /// </summary>
        public async Task<List<Order>> GetActiveSellOrdersAsync(string symbol, int limit = 1000, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.Eq(o => o.Symbol, symbol),
                Builders<Order>.Filter.Eq(o => o.Side, "SELL"),
                Builders<Order>.Filter.Eq(o => o.Status, "NEW"),
                Builders<Order>.Filter.Eq(o => o.IsWorking, true),
                Builders<Order>.Filter.Eq(o => o.IsLocked, false)
            );

            var sort = Builders<Order>.Sort
                .Ascending(o => o.Price)
                .Ascending(o => o.CreatedAt);

            return await _orderCollection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Locks orders for matching process
        /// </summary>
        public async Task LockOrdersAsync(List<Order> orders, ObjectId jobId, CancellationToken cancellationToken = default)
        {
            if (orders.Count == 0)
            {
                return;
            }

            var orderIds = orders.Select(o => o.Id).ToList();
            var now = DateTime.UtcNow;

            var updates = new List<WriteModel<Order>>();
            foreach (var order in orders)
            {
                order.IsLocked = true;
                order.LockedAt = now;
                order.LockedByJobId = jobId;

                var filter = Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq(o => o.Id, order.Id),
                    Builders<Order>.Filter.Eq(o => o.IsLocked, false)
                );

                var update = Builders<Order>.Update
                    .Set(o => o.IsLocked, true)
                    .Set(o => o.LockedAt, now)
                    .Set(o => o.LockedByJobId, jobId);

                updates.Add(new UpdateOneModel<Order>(filter, update));
            }

            if (updates.Count > 0)
            {
                await _orderCollection.BulkWriteAsync(updates, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Unlocks orders after matching process
        /// </summary>
        public async Task UnlockOrdersAsync(List<Order> orders, CancellationToken cancellationToken = default)
        {
            if (orders.Count == 0)
            {
                return;
            }

            var updates = new List<WriteModel<Order>>();
            foreach (var order in orders)
            {
                order.IsLocked = false;
                order.LockedAt = null;
                order.LockedByJobId = null;

                var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
                var update = Builders<Order>.Update
                    .Set(o => o.IsLocked, false)
                    .Set(o => o.LockedAt, null)
                    .Set(o => o.LockedByJobId, null);

                updates.Add(new UpdateOneModel<Order>(filter, update));
            }

            if (updates.Count > 0)
            {
                await _orderCollection.BulkWriteAsync(updates, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Unlocks orders that have been locked for more than the specified timeout (in seconds)
        /// </summary>
        public async Task UnlockTimedOutOrdersAsync(int timeoutSeconds = 60, CancellationToken cancellationToken = default)
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.Eq(o => o.IsLocked, true),
                Builders<Order>.Filter.Lt(o => o.LockedAt, cutoffTime)
            );

            var update = Builders<Order>.Update
                .Set(o => o.IsLocked, false)
                .Set(o => o.LockedAt, null)
                .Set(o => o.LockedByJobId, null);

            await _orderCollection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Updates an order with trade information
        /// </summary>
        public async Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
            await _orderCollection.ReplaceOneAsync(filter, order, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a batch of order updates as a transaction
        /// </summary>
        public async Task UpdateOrdersAsync(List<Order> orders, CancellationToken cancellationToken = default)
        {
            var updates = new List<WriteModel<Order>>();

            foreach (var order in orders)
            {
                var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
                updates.Add(new ReplaceOneModel<Order>(filter, order));
            }

            if (updates.Count > 0)
            {
                await _orderCollection.BulkWriteAsync(updates, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Creates new trades in the database
        /// </summary>
        public async Task CreateTradesAsync(List<Trade> trades, CancellationToken cancellationToken = default)
        {
            if (trades.Count > 0)
            {
                await _tradeCollection.InsertManyAsync(trades, cancellationToken: cancellationToken);
            }
        }
    }
}