using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    /// <summary>
    /// Extension methods for registering all API service clients
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registers all trading system API clients with the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTradingSystemServices(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<IdentityService>();
            services.AddScoped<AccountService>();
            services.AddScoped<MarketDataService>();
            services.AddScoped<TradingService>();
            services.AddScoped<RiskService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<MatchMakingService>();

            return services;
        }

        /// <summary>
        /// Registers all trading system API clients with the service collection and a custom logger
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="logger">The logger to use for all services</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTradingSystemServices(this IServiceCollection services, ILogger logger)
        {
            services.AddHttpClient();

            // Register all services with the custom logger
            services.AddScoped(sp => new IdentityService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new AccountService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new MarketDataService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new TradingService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new RiskService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new MatchMakingService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new NotificationService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            return services;
        }
    }
}