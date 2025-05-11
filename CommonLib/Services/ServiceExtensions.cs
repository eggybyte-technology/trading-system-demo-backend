using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddCommonLibServices(this IServiceCollection services)
        {
            services.AddScoped<IApiLoggingService, ApiLoggingService>();
            services.AddScoped<ILoggerService, LoggerService>();
            services.AddSingleton<MongoDbConnectionFactory>();
            // Add other common services here as needed

            return services;
        }
    }
}