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
    /// Repository for MatchingJob operations
    /// </summary>
    public class MatchingJobRepository
    {
        private readonly IMongoCollection<MatchingJob> _collection;

        /// <summary>
        /// Initializes a new instance of the MatchingJobRepository class
        /// </summary>
        public MatchingJobRepository(MongoDbConnectionFactory dbFactory)
        {
            _collection = dbFactory.GetCollection<MatchingJob>();
        }

        /// <summary>
        /// Creates a new matching job
        /// </summary>
        public async Task<MatchingJob> CreateJobAsync(MatchingJob job, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(job, cancellationToken: cancellationToken);
            return job;
        }

        /// <summary>
        /// Updates an existing matching job
        /// </summary>
        public async Task UpdateJobAsync(MatchingJob job, CancellationToken cancellationToken = default)
        {
            var filter = Builders<MatchingJob>.Filter.Eq(j => j.Id, job.Id);
            await _collection.ReplaceOneAsync(filter, job, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets recent matching jobs for a symbol
        /// </summary>
        public async Task<List<MatchingJob>> GetRecentJobsBySymbolAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
        {
            var filter = Builders<MatchingJob>.Filter.Eq(j => j.Symbol, symbol);
            var sort = Builders<MatchingJob>.Sort.Descending(j => j.StartedAt);

            return await _collection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the latest matching job stats for all symbols
        /// </summary>
        public async Task<List<MatchingJob>> GetLatestJobsAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            var sort = Builders<MatchingJob>.Sort.Descending(j => j.StartedAt);

            return await _collection.Find(Builders<MatchingJob>.Filter.Empty)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
    }
}