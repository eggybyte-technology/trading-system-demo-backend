using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Service for making HTTP requests to the API endpoints
    /// </summary>
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the HttpClientService
        /// </summary>
        public HttpClientService()
        {
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Sets the authentication token for API requests
        /// </summary>
        /// <param name="token">JWT token</param>
        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Makes a GET request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> GetAsync<TResponse>(string service, string endpoint, string queryParams = null)
        {
            var baseUrl = GetServiceBaseUrl(service);
            var url = $"{baseUrl}/{endpoint}";

            if (!string.IsNullOrEmpty(queryParams))
            {
                url += $"?{queryParams}";
            }

            var response = await _httpClient.GetAsync(url);
            return await HandleResponse<TResponse>(response);
        }

        /// <summary>
        /// Makes a POST request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="request">Request object</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string service, string endpoint, TRequest request)
        {
            var baseUrl = GetServiceBaseUrl(service);
            var url = $"{baseUrl}/{endpoint}";

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content);
            return await HandleResponse<TResponse>(response);
        }

        /// <summary>
        /// Makes a PUT request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="request">Request object</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> PutAsync<TRequest, TResponse>(string service, string endpoint, TRequest request)
        {
            var baseUrl = GetServiceBaseUrl(service);
            var url = $"{baseUrl}/{endpoint}";

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(url, content);
            return await HandleResponse<TResponse>(response);
        }

        /// <summary>
        /// Makes a DELETE request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> DeleteAsync<TResponse>(string service, string endpoint)
        {
            var baseUrl = GetServiceBaseUrl(service);
            var url = $"{baseUrl}/{endpoint}";

            var response = await _httpClient.DeleteAsync(url);
            return await HandleResponse<TResponse>(response);
        }

        /// <summary>
        /// Handles the HTTP response and converts it to the specified type
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="response">HTTP response</param>
        /// <returns>Response object</returns>
        private async Task<TResponse> HandleResponse<TResponse>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {content}");
            }

            // Parse the API standard response format
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<TResponse>>(content, _jsonOptions);

                if (apiResponse == null)
                {
                    throw new JsonException("Failed to deserialize API response");
                }

                if (apiResponse.Success)
                {
                    return apiResponse.Data;
                }

                throw new HttpRequestException($"API error: {apiResponse.Message} (Code: {apiResponse.Code})");
            }
            catch (JsonException)
            {
                // Some endpoints might not return the standard wrapper format
                // In that case, try to deserialize directly to the response type
                return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
            }
        }

        /// <summary>
        /// Gets the base URL for a service
        /// </summary>
        /// <param name="service">Service name</param>
        /// <returns>Base URL</returns>
        private string GetServiceBaseUrl(string service)
        {
            return service switch
            {
                "identity" => "http://identity.trading-system.local",
                "account" => "http://account.trading-system.local",
                "market" => "http://market-data.trading-system.local",
                "trading" => "http://trading.trading-system.local",
                "risk" => "http://risk.trading-system.local",
                "notification" => "http://notification.trading-system.local",
                _ => throw new ArgumentException($"Unknown service: {service}")
            };
        }
    }

    /// <summary>
    /// API standard response wrapper
    /// </summary>
    /// <typeparam name="T">Response data type</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Response data
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Success flag
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message (if Success is false)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Error code (if Success is false)
        /// </summary>
        public string Code { get; set; }
    }
}