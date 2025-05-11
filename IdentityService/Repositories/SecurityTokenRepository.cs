using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Services;
using CommonLib.Models.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IdentityService.Repositories
{
    /// <summary>
    /// Repository for security token operations in the database
    /// </summary>
    public class SecurityTokenRepository : ISecurityTokenRepository
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the SecurityTokenRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service dependency</param>
        public SecurityTokenRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a token by its ID
        /// </summary>
        /// <param name="id">The token ID</param>
        /// <returns>The token or null if not found</returns>
        public async Task<SecurityToken?> GetByIdAsync(ObjectId id)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                return await tokenCollection.Find(t => t.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a token by its value
        /// </summary>
        /// <param name="token">The token value</param>
        /// <returns>The token or null if not found</returns>
        public async Task<SecurityToken?> GetByTokenValueAsync(string token)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                return await tokenCollection.Find(t => t.Token == token).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetByTokenValueAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all tokens for a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of tokens</returns>
        public async Task<List<SecurityToken>> GetByUserIdAsync(ObjectId userId)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                return await tokenCollection.Find(t => t.UserId == userId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetByUserIdAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all valid (not expired, not revoked) tokens for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of valid tokens</returns>
        public async Task<List<SecurityToken>> GetValidTokensByUserIdAsync(ObjectId userId)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                var now = DateTime.UtcNow;
                return await tokenCollection
                    .Find(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetValidTokensByUserIdAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new security token
        /// </summary>
        /// <param name="token">The token to create</param>
        /// <returns>The created token with ID</returns>
        public async Task<SecurityToken> CreateAsync(SecurityToken token)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                await tokenCollection.InsertOneAsync(token);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing security token
        /// </summary>
        /// <param name="token">The token to update</param>
        /// <returns>True if updated successfully</returns>
        public async Task<bool> UpdateAsync(SecurityToken token)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                var result = await tokenCollection.ReplaceOneAsync(
                    t => t.Id == token.Id,
                    token);

                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Revokes a specific token
        /// </summary>
        /// <param name="tokenId">The token ID</param>
        /// <returns>True if revoked successfully</returns>
        public async Task<bool> RevokeAsync(ObjectId tokenId)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                var update = Builders<SecurityToken>.Update
                    .Set(t => t.IsRevoked, true);

                var result = await tokenCollection.UpdateOneAsync(
                    t => t.Id == tokenId && !t.IsRevoked,
                    update);

                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RevokeAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Revokes all tokens for a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Number of tokens revoked</returns>
        public async Task<int> RevokeAllForUserAsync(ObjectId userId)
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                var update = Builders<SecurityToken>.Update
                    .Set(t => t.IsRevoked, true);

                var result = await tokenCollection.UpdateManyAsync(
                    t => t.UserId == userId && !t.IsRevoked,
                    update);

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RevokeAllForUserAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Cleans up expired tokens
        /// </summary>
        /// <returns>The number of tokens cleaned up</returns>
        public async Task<long> CleanupExpiredTokensAsync()
        {
            try
            {
                var tokenCollection = _dbFactory.GetCollection<SecurityToken>();
                var now = DateTime.UtcNow;

                var result = await tokenCollection.DeleteManyAsync(
                    t => t.ExpiresAt < now);

                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CleanupExpiredTokensAsync: {ex.Message}");
                throw;
            }
        }
    }
}