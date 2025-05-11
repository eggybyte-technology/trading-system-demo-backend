using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace CommonLib.Services
{
    /// <summary>
    /// MongoDB database connection factory for obtaining database connections and collections
    /// </summary>
    public class MongoDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, IMongoDatabase> _databases = new();
        private MongoClient _client;

        /// <summary>
        /// Initializes a new MongoDB connection factory
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        public MongoDbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the MongoDB client
        /// </summary>
        /// <returns>MongoDB client instance</returns>
        public MongoClient GetClient()
        {
            if (_client == null)
            {
                var connectionString = _configuration["MongoDB:ConnectionString"];
                _client = new MongoClient(connectionString);
            }
            return _client;
        }

        /// <summary>
        /// Validates if the MongoDB connection is valid
        /// </summary>
        /// <returns>True if connection is valid, false otherwise</returns>
        public bool IsConnectionValid()
        {
            try
            {
                var client = GetClient();
                client.ListDatabaseNames().ToList(); // Execute a command to verify connection
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a MongoDB database connection
        /// </summary>
        /// <param name="databaseName">Database name</param>
        /// <returns>MongoDB database connection</returns>
        public IMongoDatabase GetDatabase(string databaseName)
        {
            return _databases.GetOrAdd(databaseName, name =>
            {
                var client = GetClient();
                return client.GetDatabase(name);
            });
        }

        /// <summary>
        /// Gets a MongoDB collection for the specified model type
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <returns>MongoDB collection</returns>
        public IMongoCollection<T> GetCollection<T>() where T : BaseModel, new()
        {
            var model = new T();
            var database = GetDatabase(model.Database);
            return database.GetCollection<T>(model.Collection);
        }

        /// <summary>
        /// Gets a MongoDB collection for the specified model instance
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <param name="model">Model instance</param>
        /// <returns>MongoDB collection</returns>
        public IMongoCollection<T> GetCollection<T>(T model) where T : BaseModel
        {
            var database = GetDatabase(model.Database);
            return database.GetCollection<T>(model.Collection);
        }

        /// <summary>
        /// Creates an index if it doesn't already exist using type T's database and collection
        /// </summary>
        /// <typeparam name="T">The document type</typeparam>
        /// <param name="keys">The index keys definition</param>
        /// <param name="unique">Whether the index should enforce uniqueness</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CreateIndexAsync<T>(IndexKeysDefinition<T> keys, bool unique) where T : BaseModel, new()
        {
            var model = new T();
            await CreateIndexIfNotExistsAsync<T>(model.Collection, keys, unique, model.Database);
        }

        /// <summary>
        /// Creates an index if it doesn't already exist
        /// </summary>
        /// <typeparam name="T">The document type</typeparam>
        /// <param name="collectionName">The collection name</param>
        /// <param name="keys">The index keys definition</param>
        /// <param name="unique">Whether the index should enforce uniqueness</param>
        /// <param name="database">The database name</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task CreateIndexIfNotExistsAsync<T>(
            string collectionName,
            IndexKeysDefinition<T> keys,
            bool unique,
            string database)
        {
            try
            {
                var collection = GetCollection<T>(collectionName, database);

                // Get existing indexes
                var indexCursor = await collection.Indexes.ListAsync();
                var indexes = await indexCursor.ToListAsync();

                // Create index definition
                var indexOptions = new CreateIndexOptions { Unique = unique };
                var indexModel = new CreateIndexModel<T>(keys, indexOptions);

                // Check if a similar index already exists
                // Note: This is a simplified check and might not catch all identical indexes
                bool indexExists = false;
                foreach (var index in indexes)
                {
                    if (index.Contains("key") && index["key"].ToString().Contains(keys.ToString()))
                    {
                        indexExists = true;
                        break;
                    }
                }

                if (!indexExists)
                {
                    await collection.Indexes.CreateOneAsync(indexModel);
                }
            }
            catch
            {
                // Don't rethrow - allow initialization to continue with other collections
            }
        }

        /// <summary>
        /// Gets a MongoDB collection for the specified collection and database name
        /// </summary>
        /// <typeparam name="T">Document type</typeparam>
        /// <param name="collectionName">Collection name</param>
        /// <param name="databaseName">Database name</param>
        /// <returns>MongoDB collection</returns>
        public IMongoCollection<T> GetCollection<T>(string collectionName, string databaseName)
        {
            var database = GetDatabase(databaseName);
            return database.GetCollection<T>(collectionName);
        }

        /// <summary>
        /// Ensures that a collection exists by creating it if needed
        /// </summary>
        /// <typeparam name="T">The document type</typeparam>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task EnsureCollectionExistsAsync<T>() where T : BaseModel, new()
        {
            var model = new T();
            await EnsureCollectionExistsAsync(model.Collection, model.Database);
        }

        /// <summary>
        /// Ensures that a collection exists by creating it if needed
        /// </summary>
        /// <param name="collectionName">The collection name</param>
        /// <param name="database">The database name</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task EnsureCollectionExistsAsync(string collectionName, string database)
        {
            try
            {
                var client = GetClient();
                var collections = await client.GetDatabase(database).ListCollectionNamesAsync();
                var collectionsList = await collections.ToListAsync();

                if (!collectionsList.Contains(collectionName))
                {
                    await client.GetDatabase(database).CreateCollectionAsync(collectionName);
                }
            }
            catch
            {
                // Don't rethrow - allow initialization to continue with other collections
            }
        }
    }
}