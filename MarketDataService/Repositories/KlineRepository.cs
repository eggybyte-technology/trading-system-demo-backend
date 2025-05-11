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
        public async Task<List<Kline>> GetKlinesAsync(string symbolName, string interval, DateTime? startTime = null, DateTime? endTime = null, int limit = 500)
        {
            var filterBuilder = Builders<Kline>.Filter;
            var filter = filterBuilder.Eq(k => k.Symbol, symbolName) & filterBuilder.Eq(k => k.Interval, interval);

            if (startTime.HasValue)
            {
                filter = filter & filterBuilder.Gte(k => k.OpenTime, startTime.Value);
            }

            if (endTime.HasValue)
            {
                filter = filter & filterBuilder.Lte(k => k.OpenTime, endTime.Value);
            }

            return await _klines.Find(filter)
                .Sort(Builders<Kline>.Sort.Ascending(k => k.OpenTime))
                .Limit(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Kline> GetKlineByIdAsync(ObjectId id)
        {
            return await _klines.Find(k => k.Id == id).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<Kline> CreateKlineAsync(Kline kline)
        {
            await _klines.InsertOneAsync(kline);
            return kline;
        }

        /// <inheritdoc />
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