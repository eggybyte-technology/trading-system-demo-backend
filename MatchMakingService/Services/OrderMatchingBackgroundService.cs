using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchMakingService.Services
{
    /// <summary>
    /// Background service that runs the order matching process at regular intervals
    /// </summary>
    public class OrderMatchingBackgroundService : BackgroundService
    {
        private readonly ILogger<OrderMatchingBackgroundService> _logger;
        private readonly OrderMatchingService _matchingService;
        private readonly IConfiguration _configuration;
        private int _matchIntervalMs;

        /// <summary>
        /// Initializes a new instance of the OrderMatchingBackgroundService class
        /// </summary>
        public OrderMatchingBackgroundService(
            ILogger<OrderMatchingBackgroundService> logger,
            OrderMatchingService matchingService,
            IConfiguration configuration)
        {
            _logger = logger;
            _matchingService = matchingService;
            _configuration = configuration;
            _matchIntervalMs = _configuration.GetValue<int>("MatchMaking:DefaultMatchIntervalMs", 1000);
        }

        /// <summary>
        /// Executes the background service
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Matching Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run the order matching process
                    await _matchingService.ProcessAllPendingOrdersAsync(stoppingToken);

                    // Wait for the configured interval before processing again
                    // Reload the interval from configuration in case it was changed
                    _matchIntervalMs = _configuration.GetValue<int>("MatchMaking:DefaultMatchIntervalMs", _matchIntervalMs);
                    await Task.Delay(_matchIntervalMs, stoppingToken);
                }
                catch (Exception ex) when (!(ex is TaskCanceledException && stoppingToken.IsCancellationRequested))
                {
                    _logger.LogError(ex, "Error occurred while processing orders");

                    // Wait a bit longer before retrying after an error
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("Order Matching Service stopping...");
        }
    }
}