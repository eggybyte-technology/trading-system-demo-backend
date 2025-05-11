using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CommonLib.Services
{
    /// <summary>
    /// Service for making HTTP requests to other microservices
    /// </summary>
    public interface IHttpClientService
    {
        /// <summary>
        /// Gets the base URL for a service
        /// </summary>
        /// <param name="serviceName">Name of the service (e.g., "IdentityService")</param>
        /// <returns>Base URL of the service</returns>
        string GetServiceUrl(string serviceName);

        /// <summary>
        /// Makes a GET request to a service
        /// </summary>
        /// <typeparam name="T">Response type</typeparam>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="endpoint">Endpoint to call</param>
        /// <param name="token">Optional authorization token</param>
        /// <returns>Response data</returns>
        Task<T?> GetAsync<T>(string serviceName, string endpoint, string? token = null);

        /// <summary>
        /// Makes a POST request to a service
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="endpoint">Endpoint to call</param>
        /// <param name="data">Request data</param>
        /// <param name="token">Optional authorization token</param>
        /// <returns>Response data</returns>
        Task<TResponse?> PostAsync<TRequest, TResponse>(string serviceName, string endpoint, TRequest data, string? token = null);

        /// <summary>
        /// Makes a PUT request to a service
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="endpoint">Endpoint to call</param>
        /// <param name="data">Request data</param>
        /// <param name="token">Optional authorization token</param>
        /// <returns>Response data</returns>
        Task<TResponse?> PutAsync<TRequest, TResponse>(string serviceName, string endpoint, TRequest data, string? token = null);

        /// <summary>
        /// Makes a DELETE request to a service
        /// </summary>
        /// <typeparam name="T">Response type</typeparam>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="endpoint">Endpoint to call</param>
        /// <param name="token">Optional authorization token</param>
        /// <returns>Response data</returns>
        Task<T?> DeleteAsync<T>(string serviceName, string endpoint, string? token = null);
    }

    /// <summary>
    /// Implementation of HTTP client service
    /// </summary>
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the HTTP client service
        /// </summary>
        public HttpClientService(HttpClient httpClient, IConfiguration configuration, ILoggerService logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _jsonOptions.ConfigureMongoDbJsonOptions();
        }

        /// <inheritdoc/>
        public string GetServiceUrl(string serviceName)
        {
            var serviceUrl = _configuration[$"ServiceUrls:{serviceName}"];

            if (string.IsNullOrEmpty(serviceUrl))
            {
                throw new InvalidOperationException($"URL for service '{serviceName}' not found in configuration");
            }

            return serviceUrl;
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string serviceName, string endpoint, string? token = null)
        {
            try
            {
                var url = $"{GetServiceUrl(serviceName)}/{endpoint.TrimStart('/')}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                AddAuthorizationHeader(request, token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GET request failed: {ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string serviceName, string endpoint, TRequest data, string? token = null)
        {
            try
            {
                var url = $"{GetServiceUrl(serviceName)}/{endpoint.TrimStart('/')}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                AddAuthorizationHeader(request, token);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"POST request failed: {ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string serviceName, string endpoint, TRequest data, string? token = null)
        {
            try
            {
                var url = $"{GetServiceUrl(serviceName)}/{endpoint.TrimStart('/')}";
                var request = new HttpRequestMessage(HttpMethod.Put, url);

                AddAuthorizationHeader(request, token);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"PUT request failed: {ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<T?> DeleteAsync<T>(string serviceName, string endpoint, string? token = null)
        {
            try
            {
                var url = $"{GetServiceUrl(serviceName)}/{endpoint.TrimStart('/')}";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                AddAuthorizationHeader(request, token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"DELETE request failed: {ex.Message}", ex);
                throw;
            }
        }

        private void AddAuthorizationHeader(HttpRequestMessage request, string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}