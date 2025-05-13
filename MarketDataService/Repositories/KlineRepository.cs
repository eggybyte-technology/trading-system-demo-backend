using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// MongoDB implementation of IKlineRepository
    /// </summary>
    public class KlineRepository : IKlineRepository
    {
        private readonly IMongoCollection<Kline> _klines;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Constructor for KlineRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public KlineRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _logger = logger;
            _klines = dbFactory.GetCollection<Kline>();

            try
            {
                // Create composite index on Symbol, Interval, and OpenTime
                var indexKeysDefinition = Builders<Kline>.IndexKeys
                    .Ascending(k => k.Symbol)
                    .Ascending(k => k.Interval)
                    .Ascending(k => k.OpenTime);

                var indexOptions = new CreateIndexOptions { Unique = true };
                var indexModel = new CreateIndexModel<Kline>(indexKeysDefinition, indexOptions);
                _klines.Indexes.CreateOne(indexModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating index on klines collection: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Kline?> GetKlineAsync(string symbol, string interval, DateTime startTime)
        {
            var filter = Builders<Kline>.Filter
                .Eq(k => k.Symbol, symbol) &
                Builders<Kline>.Filter.Eq(k => k.Interval, interval) &
                Builders<Kline>.Filter.Eq(k => k.OpenTime, startTime);

            return await _klines.Find(filter).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<List<Kline>> GetKlinesAsync(string symbol, string interval, DateTime startTime, DateTime endTime, int limit = 500)
        {
            var filterBuilder = Builders<Kline>.Filter;
            var filter = filterBuilder.Eq(k => k.Symbol, symbol) &
                         filterBuilder.Eq(k => k.Interval, interval) &
                         filterBuilder.Gte(k => k.OpenTime, startTime) &
                         filterBuilder.Lte(k => k.OpenTime, endTime);

            return await _klines.Find(filter)
                .Sort(Builders<Kline>.Sort.Ascending(k => k.OpenTime))
                .Limit(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Kline?> GetLatestKlineAsync(string symbol, string interval)
        {
            var filter = Builders<Kline>.Filter
                .Eq(k => k.Symbol, symbol) &
                Builders<Kline>.Filter.Eq(k => k.Interval, interval);

            return await _klines.Find(filter)
                .Sort(Builders<Kline>.Sort.Descending(k => k.OpenTime))
                .Limit(1)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<Kline> UpsertKlineAsync(Kline kline)
        {
            var filter = Builders<Kline>.Filter
                .Eq(k => k.Symbol, kline.Symbol) &
                Builders<Kline>.Filter.Eq(k => k.Interval, kline.Interval) &
                Builders<Kline>.Filter.Eq(k => k.OpenTime, kline.OpenTime);

            var options = new ReplaceOptions { IsUpsert = true };
            await _klines.ReplaceOneAsync(filter, kline, options);
            return kline;
        }

        /// <inheritdoc />
        public async Task<int> DeleteKlinesAsync(string symbol)
        {
            var filter = Builders<Kline>.Filter.Eq(k => k.Symbol, symbol);
            var result = await _klines.DeleteManyAsync(filter);
            return (int)result.DeletedCount;
        }

        // Implementation for internal use
        public async Task<int> UpsertKlinesAsync(List<Kline> klines)
        {
            if (klines == null || klines.Count == 0)
            {
                return 0;
            }

            var bulkOps = new List<WriteModel<Kline>>();

            foreach (var kline in klines)
            {
                var filter = Builders<Kline>.Filter
                    .Eq(k => k.Symbol, kline.Symbol) &
                    Builders<Kline>.Filter.Eq(k => k.Interval, kline.Interval) &
                    Builders<Kline>.Filter.Eq(k => k.OpenTime, kline.OpenTime);

                var upsert = new ReplaceOneModel<Kline>(filter, kline) { IsUpsert = true };
                bulkOps.Add(upsert);
            }

            var result = await _klines.BulkWriteAsync(bulkOps);
            return (int)(result.InsertedCount + result.ModifiedCount);
        }
    }
}