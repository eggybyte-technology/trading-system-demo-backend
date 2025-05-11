using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson;
using CommonLib.Models.Identity;

namespace IdentityService.Repositories
{
    /// <summary>
    /// Interface for user repository operations
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <returns>The user or null if not found</returns>
        Task<User?> GetByIdAsync(ObjectId id);

        /// <summary>
        /// Gets a user by their email address
        /// </summary>
        /// <param name="email">The email address</param>
        /// <returns>The user or null if not found</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Gets a user by their username
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns>The user or null if not found</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">The user to create</param>
        /// <returns>The created user with ID</returns>
        Task<User> CreateAsync(User user);

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateAsync(User user);

        /// <summary>
        /// Checks if a username is already taken
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns>True if the username is already in use</returns>
        Task<bool> IsUsernameTakenAsync(string username);

        /// <summary>
        /// Checks if an email is already registered
        /// </summary>
        /// <param name="email">The email to check</param>
        /// <returns>True if the email is already in use</returns>
        Task<bool> IsEmailRegisteredAsync(string email);

        /// <summary>
        /// Gets all users in the system
        /// </summary>
        /// <param name="skip">The number of users to skip</param>
        /// <param name="limit">The maximum number of users to return</param>
        /// <returns>A list of users</returns>
        Task<List<User>> GetAllAsync(int skip = 0, int limit = 50);
    }
}