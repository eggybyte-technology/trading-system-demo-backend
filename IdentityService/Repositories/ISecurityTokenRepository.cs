using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson;
using CommonLib.Models.Identity;

namespace IdentityService.Repositories
{
    /// <summary>
    /// Interface for security token repository operations
    /// </summary>
    public interface ISecurityTokenRepository
    {
        /// <summary>
        /// Gets a token by its ID
        /// </summary>
        /// <param name="id">The token ID</param>
        /// <returns>The token or null if not found</returns>
        Task<SecurityToken?> GetByIdAsync(ObjectId id);

        /// <summary>
        /// Gets a token by its value
        /// </summary>
        /// <param name="token">The token value</param>
        /// <returns>The token or null if not found</returns>
        Task<SecurityToken?> GetByTokenValueAsync(string token);

        /// <summary>
        /// Gets all tokens for a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of tokens</returns>
        Task<List<SecurityToken>> GetByUserIdAsync(ObjectId userId);

        /// <summary>
        /// Gets all valid (not expired, not revoked) tokens for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of valid tokens</returns>
        Task<List<SecurityToken>> GetValidTokensByUserIdAsync(ObjectId userId);

        /// <summary>
        /// Creates a new token
        /// </summary>
        /// <param name="token">The token to create</param>
        /// <returns>The created token with ID</returns>
        Task<SecurityToken> CreateAsync(SecurityToken token);

        /// <summary>
        /// Updates an existing token
        /// </summary>
        /// <param name="token">The token to update</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateAsync(SecurityToken token);

        /// <summary>
        /// Revokes a specific token
        /// </summary>
        /// <param name="tokenId">The token ID</param>
        /// <returns>True if revoked successfully</returns>
        Task<bool> RevokeAsync(ObjectId tokenId);

        /// <summary>
        /// Revokes all tokens for a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Number of tokens revoked</returns>
        Task<int> RevokeAllForUserAsync(ObjectId userId);
    }
}