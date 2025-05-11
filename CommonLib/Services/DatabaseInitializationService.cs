using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommonLib.Models;
using CommonLib.Models.Account;
using CommonLib.Models.Identity;
using CommonLib.Models.Market;
using CommonLib.Models.Notification;
using CommonLib.Models.Risk;
using CommonLib.Models.Trading;
using MongoDB.Bson;

namespace CommonLib.Services
{
    /// <summary>
    /// Service for initializing database collections and indexes
    /// </summary>
    public class DatabaseInitializationService
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the DatabaseInitializationService
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory for database operations</param>
        /// <param name="logger">Logger service</param>
        public DatabaseInitializationService(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes all database collections and indexes
        /// This method can be safely called multiple times
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAllDatabasesAsync()
        {
            _logger.LogInformation("Starting database initialization...");

            // Verify MongoDB connection is valid before proceeding
            if (!_dbFactory.IsConnectionValid())
            {
                string errorMessage = "MongoDB connection failed. Cannot initialize databases.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                // Initialize all collections and indexes using reflection
                await InitializeAllModelsAsync();
                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during database initialization: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes all database collections and indexes automatically using reflection
        /// </summary>
        private async Task InitializeAllModelsAsync()
        {
            _logger.LogInformation("Starting automatic database initialization...");

            // Get all types that inherit from BaseModel
            var assembly = Assembly.GetExecutingAssembly();
            var modelTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseModel).IsAssignableFrom(t))
                .ToList();

            // Create collections
            var createdCollections = new List<string>();
            foreach (var modelType in modelTypes)
            {
                try
                {
                    // Create instance of model and ensure collection exists
                    var model = (BaseModel)Activator.CreateInstance(modelType);
                    await _dbFactory.EnsureCollectionExistsAsync(model.Collection, model.Database);
                    createdCollections.Add($"{model.Database}.{model.Collection}");

                    _logger.LogInformation($"Created collection: {model.Database}.{model.Collection}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not initialize collection for {modelType.Name}: {ex.Message}");
                }
            }

            // Get all types that inherit from IndexedModel<>
            var createdIndexes = new List<string>();
            var indexedTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                      t.GetInterfaces().Any(i => i.IsGenericType &&
                      i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) &&
                      typeof(BaseModel).IsAssignableFrom(t))
                .ToList();

            // A better way to get IndexedModel types
            var indexedModelTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                      t.BaseType != null &&
                      t.BaseType.IsGenericType &&
                      t.BaseType.GetGenericTypeDefinition() == typeof(IndexedModel<>))
                .ToList();

            // Create indexes
            foreach (var modelType in indexedModelTypes)
            {
                try
                {
                    // Create instance of model
                    var model = Activator.CreateInstance(modelType);

                    // Get the GetIndexes method
                    var getIndexesMethod = modelType.GetMethod("GetIndexes");
                    if (getIndexesMethod == null)
                    {
                        _logger.LogWarning($"Could not find GetIndexes method for {modelType.Name}");
                        continue;
                    }

                    // Invoke GetIndexes to get the list of indexes
                    var indexes = getIndexesMethod.Invoke(model, null) as System.Collections.IEnumerable;
                    if (indexes == null)
                    {
                        _logger.LogWarning($"GetIndexes returned null for {modelType.Name}");
                        continue;
                    }

                    // Create each index
                    foreach (var index in indexes)
                    {
                        try
                        {
                            // Get the tuple properties using reflection
                            var tupleType = index.GetType();
                            var item1Property = tupleType.GetProperty("Item1");
                            var item2Property = tupleType.GetProperty("Item2");

                            var indexDefinition = item1Property?.GetValue(index);
                            var isUnique = (bool)(item2Property?.GetValue(index) ?? false);

                            // Use generic method to create the index
                            Type collectionType = modelType.BaseType.GetGenericArguments()[0];

                            // Get base model properties to log database and collection name
                            var baseModel = model as BaseModel;
                            var databaseName = baseModel.Database;
                            var collectionName = baseModel.Collection;

                            // Create dynamic method call based on type
                            var createIndexMethod = typeof(MongoDbConnectionFactory)
                                .GetMethod("CreateIndexAsync")
                                .MakeGenericMethod(collectionType);

                            await (Task)createIndexMethod.Invoke(_dbFactory, new[] { indexDefinition, isUnique });

                            // Log index creation
                            createdIndexes.Add($"{databaseName}.{collectionName} - {indexDefinition}");
                            _logger.LogInformation($"Created index on {databaseName}.{collectionName}: {indexDefinition}, Unique: {isUnique}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Error creating index in {modelType.Name}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not initialize indexes for {modelType.Name}: {ex.Message}");
                }
            }

            // Log summary of all created collections and indexes
            _logger.LogInformation($"Created {createdCollections.Count} collections:");
            foreach (var collection in createdCollections)
            {
                _logger.LogInformation($"  - {collection}");
            }

            _logger.LogInformation($"Created {createdIndexes.Count} indexes:");
            foreach (var index in createdIndexes)
            {
                _logger.LogInformation($"  - {index}");
            }
        }
    }
}