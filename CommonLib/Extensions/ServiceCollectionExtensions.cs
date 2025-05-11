using CommonLib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Extensions
{
    /// <summary>
    /// Extension methods for service collection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add MongoDB connection factory to services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMongoDb(this IServiceCollection services)
        {
            services.AddSingleton<MongoDbConnectionFactory>();
            return services;
        }
    }
}