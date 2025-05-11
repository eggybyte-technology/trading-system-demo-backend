using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SimulationTest.Helpers
{
    /// <summary>
    /// Factory for creating and managing HTTP clients
    /// </summary>
    public class HttpClientFactory
    {
        private readonly ConcurrentDictionary<string, HttpClient> _clients = new ConcurrentDictionary<string, HttpClient>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HttpClient>> _userClients = new ConcurrentDictionary<string, ConcurrentDictionary<string, HttpClient>>();
        private readonly Dictionary<string, string> _serviceUrls = new Dictionary<string, string>();
        private int _timeout = 30;

        /// <summary>
        /// Configures the HTTP client factory with a timeout value
        /// </summary>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        public void Configure(int timeoutSeconds)
        {
            _timeout = timeoutSeconds;
        }

        /// <summary>
        /// Initializes HTTP clients for all services
        /// </summary>
        /// <param name="tradingHost">Trading service URL</param>
        /// <param name="identityHost">Identity service URL</param>
        /// <param name="marketDataHost">Market data service URL</param>
        /// <param name="accountHost">Account service URL</param>
        /// <param name="riskHost">Risk service URL</param>
        /// <param name="notificationHost">Notification service URL</param>
        /// <param name="timeout">Timeout in seconds</param>
        public void InitializeHttpClients(
            string tradingHost,
            string identityHost,
            string marketDataHost,
            string accountHost,
            string riskHost,
            string notificationHost,
            int timeout)
        {
            // Set timeout
            _timeout = timeout;

            // Configure service URLs
            var serviceUrls = new Dictionary<string, string>
            {
                { "trading", tradingHost },
                { "identity", identityHost },
                { "market-data", marketDataHost },
                { "account", accountHost },
                { "risk", riskHost },
                { "notification", notificationHost }
            };

            ConfigureServiceUrls(serviceUrls);

            // Create clients for each service
            foreach (var service in serviceUrls.Keys)
            {
                GetClient(service);
            }
        }

        /// <summary>
        /// Configures the service URLs for the HTTP client factory
        /// </summary>
        /// <param name="serviceUrls">Dictionary of service names and URLs</param>
        public void ConfigureServiceUrls(Dictionary<string, string> serviceUrls)
        {
            foreach (var serviceUrl in serviceUrls)
            {
                _serviceUrls[serviceUrl.Key] = serviceUrl.Value;
            }
        }

        /// <summary>
        /// Gets the URL for a specific service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <returns>The service URL or null if not found</returns>
        public string GetServiceUrl(string serviceName)
        {
            return _serviceUrls.TryGetValue(serviceName, out var url) ? url : null;
        }

        /// <summary>
        /// Gets or creates an HTTP client for a service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <returns>The HTTP client</returns>
        public HttpClient GetClient(string serviceName)
        {
            return _clients.GetOrAdd(serviceName, _ =>
            {
                var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_timeout)
                };

                if (_serviceUrls.TryGetValue(serviceName, out var baseUrl))
                {
                    client.BaseAddress = new Uri(baseUrl);
                }

                return client;
            });
        }

        /// <summary>
        /// Gets or creates an HTTP client for a user and service
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="serviceName">The service name</param>
        /// <returns>The HTTP client</returns>
        public HttpClient GetUserClient(string userId, string serviceName)
        {
            var userClientDict = _userClients.GetOrAdd(userId, _ => new ConcurrentDictionary<string, HttpClient>());

            return userClientDict.GetOrAdd(serviceName, _ =>
            {
                var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_timeout)
                };

                if (_serviceUrls.TryGetValue(serviceName, out var baseUrl))
                {
                    client.BaseAddress = new Uri(baseUrl);
                }

                return client;
            });
        }

        /// <summary>
        /// Sets the authentication token for a user's clients
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="token">The authentication token</param>
        public void SetUserAuthToken(string userId, string token)
        {
            if (!_userClients.TryGetValue(userId, out var userClientDict))
            {
                userClientDict = new ConcurrentDictionary<string, HttpClient>();
                _userClients[userId] = userClientDict;
            }

            foreach (var client in userClientDict.Values)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}