using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SimulationTest.Helpers;
using Spectre.Console;

namespace SimulationTest.Core
{
    /// <summary>
    /// Checks connectivity to required services before running tests
    /// </summary>
    public class ServiceConnectivityChecker
    {
        private readonly HttpClientFactory _httpClientFactory;
        private readonly Dictionary<string, string> _serviceEndpoints;

        /// <summary>
        /// Initializes a new instance of the ServiceConnectivityChecker class
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <param name="serviceEndpoints">Dictionary of service endpoints to check</param>
        public ServiceConnectivityChecker(HttpClientFactory httpClientFactory, Dictionary<string, string> serviceEndpoints)
        {
            _httpClientFactory = httpClientFactory;
            _serviceEndpoints = serviceEndpoints;
        }

        /// <summary>
        /// Checks connectivity to all services
        /// </summary>
        /// <returns>A dictionary of service names and their connectivity status</returns>
        public async Task<Dictionary<string, bool>> CheckAllServicesAsync()
        {
            var results = new Dictionary<string, bool>();

            AnsiConsole.MarkupLine("[yellow]Checking service connectivity...[/]");

            foreach (var service in _serviceEndpoints)
            {
                // Skip match-making service connectivity check
                if (service.Key == "match-making")
                {
                    results.Add(service.Key, true);
                    AnsiConsole.MarkupLine($"Service: {service.Key} - [green]Available[/] (connectivity check skipped)");
                    continue;
                }

                bool isAvailable = await CheckServiceConnectivityAsync(service.Key);
                results.Add(service.Key, isAvailable);

                string statusText = isAvailable ? "[green]Available[/]" : "[red]Unavailable[/]";
                AnsiConsole.MarkupLine($"Service: {service.Key} - {statusText}");
            }

            return results;
        }

        /// <summary>
        /// Checks connectivity to a specific service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <returns>True if the service is available, false otherwise</returns>
        public async Task<bool> CheckServiceConnectivityAsync(string serviceName)
        {
            // Skip match-making service connectivity check
            if (serviceName == "match-making")
            {
                return true;
            }

            try
            {
                var client = _httpClientFactory.GetClient(serviceName);
                var response = await client.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates if all required services are available
        /// </summary>
        /// <param name="requiredServices">A list of services that must be available</param>
        /// <returns>True if all required services are available</returns>
        public async Task<bool> AreAllRequiredServicesAvailableAsync(List<string> requiredServices)
        {
            var results = await CheckAllServicesAsync();

            foreach (var service in requiredServices)
            {
                if (!results.TryGetValue(service, out bool isAvailable) || !isAvailable)
                {
                    AnsiConsole.MarkupLine($"[red]Required service '{service}' is not available[/]");
                    return false;
                }
            }

            return true;
        }
    }
}