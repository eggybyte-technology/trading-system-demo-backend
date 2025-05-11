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
    /// Repository for user operations in the database
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the UserRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service dependency</param>
        public UserRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <returns>The user or null if not found</returns>
        public async Task<User?> GetByIdAsync(ObjectId id)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                return await userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a user by their email address
        /// </summary>
        /// <param name="email">The email address</param>
        /// <returns>The user or null if not found</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                return await userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetByEmailAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a user by their username
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns>The user or null if not found</returns>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                return await userCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetByUsernameAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">The user to create</param>
        /// <returns>The created user with ID</returns>
        public async Task<User> CreateAsync(User user)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await userCollection.InsertOneAsync(user);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <returns>True if updated successfully</returns>
        public async Task<bool> UpdateAsync(User user)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                user.UpdatedAt = DateTime.UtcNow;
                var result = await userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if a username is already taken
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns>True if the username is already in use</returns>
        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                return await userCollection.Find(u => u.Username == username).AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in IsUsernameTakenAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if an email is already registered
        /// </summary>
        /// <param name="email">The email to check</param>
        /// <returns>True if the email is already in use</returns>
        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                return await userCollection.Find(u => u.Email == email).AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in IsEmailRegisteredAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all users in the system
        /// </summary>
        /// <param name="skip">The number of users to skip</param>
        /// <param name="limit">The maximum number of users to return</param>
        /// <returns>A list of users</returns>
        public async Task<List<User>> GetAllAsync(int skip = 0, int limit = 50)
        {
            try
            {
                var userCollection = _dbFactory.GetCollection<User>();
                return await userCollection.Find(_ => true)
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllAsync: {ex.Message}");
                throw;
            }
        }
    }
}