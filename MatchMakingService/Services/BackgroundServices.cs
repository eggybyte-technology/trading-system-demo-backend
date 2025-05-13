using System;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatchMakingService.Services
{
    /// <summary>
    /// Background service to run the matching engine periodically
    /// </summary>
    public class MatchingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerService _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(1);

        public MatchingBackgroundService(
            IServiceProvider serviceProvider,
            ILoggerService logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Matching Background Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // We don't need to do anything here as the matching service itself
                    // has its own timer for scheduling matches. This background service
                    // is just to keep the application running.
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, just break the loop
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in MatchingBackgroundService: {ex.Message}");

                    // Wait a bit before retrying to prevent tight error loops
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Matching Background Service stopping");
        }
    }

    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add background services for matching engine
        /// </summary>
        public static IServiceCollection AddMatchingBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<MatchingBackgroundService>();
            return services;
        }
    }
}