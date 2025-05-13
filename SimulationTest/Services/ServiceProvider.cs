using System;
using Microsoft.Extensions.DependencyInjection;

namespace SimulationTest.Services
{
    /// <summary>
    /// Service provider for managing API services
    /// </summary>
    public class ServiceProvider
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes the service provider
        /// </summary>
        public static void Initialize()
        {
            var services = new ServiceCollection();

            // Register HTTP client service
            services.AddSingleton<IHttpClientService, HttpClientService>();

            // Register API services
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IMarketService, MarketService>();
            services.AddScoped<ITradingService, TradingService>();
            services.AddScoped<IRiskService, RiskService>();
            services.AddScoped<INotificationService, NotificationService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Gets a service from the service provider
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance</returns>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                Initialize();
            }

            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets all registered services
        /// </summary>
        /// <returns>Service provider</returns>
        public static IServiceProvider GetServices()
        {
            if (_serviceProvider == null)
            {
                Initialize();
            }

            return _serviceProvider;
        }
    }
}