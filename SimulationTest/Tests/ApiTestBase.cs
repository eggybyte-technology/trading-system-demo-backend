using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommonLib.Models;
using CommonLib.Models.Identity;
using Microsoft.Extensions.Configuration;
using SimulationTest.Helpers;
using SimulationTest.Core;
using MongoDB.Bson;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Base class for API tests that provides common functionality
    /// </summary>
    public abstract class ApiTestBase : IDisposable
    {
        protected readonly HttpClientFactory _httpClientFactory;
        protected readonly IConfiguration _configuration;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected SecurityToken? _token;
        protected string? _userId;

        /// <summary>
        /// Initializes a new instance of the ApiTestBase class
        /// </summary>
        protected ApiTestBase()
        {
            // Load configuration
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            // Setup HTTP client factory
            _httpClientFactory = new HttpClientFactory();
            _httpClientFactory.Configure(
                int.Parse(_configuration["TestSettings:TestTimeout"] ?? "30"));

            // Configure service URLs
            _httpClientFactory.ConfigureServiceUrls(new Dictionary<string, string>
            {
                { "identity", _configuration["SimulationSettings:IdentityHost"] ?? "http://identity.trading-system.local" },
                { "trading", _configuration["SimulationSettings:TradingHost"] ?? "http://trading.trading-system.local" },
                { "market-data", _configuration["SimulationSettings:MarketDataHost"] ?? "http://market-data.trading-system.local" },
                { "account", _configuration["SimulationSettings:AccountHost"] ?? "http://account.trading-system.local" },
                { "risk", _configuration["SimulationSettings:RiskHost"] ?? "http://risk.trading-system.local" },
                { "notification", _configuration["SimulationSettings:NotificationHost"] ?? "http://notification.trading-system.local" },
                { "match-making", _configuration["SimulationSettings:MatchMakingHost"] ?? "http://match-making.trading-system.local" }
            });

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Authenticates with the Identity Service and gets an authorization token
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <returns>A security token from the Identity Service</returns>
        protected async Task<SecurityToken?> AuthenticateAsync(string username, string password)
        {
            var client = _httpClientFactory.GetClient("identity");

            // First try to login with existing credentials
            var loginRequest = new LoginRequest
            {
                Email = $"{username}@example.com",
                Password = password
            };

            var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);

            if (!loginResponse.IsSuccessStatusCode)
            {
                // If login fails, try to register a new user
                var registerRequest = new RegisterRequest
                {
                    Username = username,
                    Password = password,
                    Email = $"{username}@example.com"
                };

                var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
                if (!registerResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Registration failed: {registerResponse.StatusCode}");
                    return null;
                }

                // Now try to login again
                loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
                if (!loginResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Login failed: {loginResponse.StatusCode}");
                    return null;
                }
            }

            var response = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<SecurityToken>>(_jsonOptions);
            if (response == null || !response.Success || response.Data == null)
            {
                Console.WriteLine($"Failed to parse response or request failed: {response?.Message}");
                return null;
            }

            _token = response.Data;
            _userId = response.Data.UserId.ToString();

            return response.Data;
        }

        /// <summary>
        /// Creates an HTTP client with authorization header
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <returns>An HTTP client with authorization header</returns>
        protected HttpClient CreateAuthorizedClient(string serviceName)
        {
            if (_token == null)
                throw new InvalidOperationException("You must authenticate first by calling AuthenticateAsync before using this method");

            var client = _httpClientFactory.GetClient(serviceName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.Token);
            return client;
        }

        /// <summary>
        /// Ensures the authentication token exists or authenticates with default credentials
        /// </summary>
        protected async Task EnsureAuthenticatedAsync()
        {
            if (_token == null)
            {
                throw new InvalidOperationException(
                    "No authentication token available. Tests must be properly sequenced to run identity tests first " +
                    "(Register_WithValidData_ShouldSucceed followed by Login_WithValidCredentials_ShouldReturnToken).");
            }
        }

        /// <summary>
        /// Sends a GET request to the specified endpoint and deserializes the response
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to</typeparam>
        /// <param name="serviceName">The service name</param>
        /// <param name="endpoint">The endpoint to send the request to</param>
        /// <param name="requiresAuth">Whether the endpoint requires authentication</param>
        /// <returns>The deserialized response</returns>
        protected async Task<T?> GetAsync<T>(string serviceName, string endpoint, bool requiresAuth = true)
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            var response = await client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
            if (apiResponse == null || !apiResponse.Success)
            {
                throw new InvalidOperationException($"API request failed: {apiResponse?.Message}");
            }

            return apiResponse.Data;
        }

        /// <summary>
        /// Sends a POST request to the specified endpoint and deserializes the response
        /// </summary>
        /// <typeparam name="TRequest">The type of the request body</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
        /// <param name="serviceName">The service name</param>
        /// <param name="endpoint">The endpoint to send the request to</param>
        /// <param name="requestBody">The request body</param>
        /// <param name="requiresAuth">Whether the endpoint requires authentication</param>
        /// <returns>The deserialized response</returns>
        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string serviceName,
            string endpoint,
            TRequest requestBody,
            bool requiresAuth = true)
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            var response = await client.PostAsJsonAsync(endpoint, requestBody);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
            if (apiResponse == null || !apiResponse.Success)
            {
                throw new InvalidOperationException($"API request failed: {apiResponse?.Message}");
            }

            return apiResponse.Data;
        }

        /// <summary>
        /// Sends a PUT request to the specified endpoint and deserializes the response
        /// </summary>
        /// <typeparam name="TRequest">The type of the request body</typeparam>
        /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
        /// <param name="serviceName">The service name</param>
        /// <param name="endpoint">The endpoint to send the request to</param>
        /// <param name="requestBody">The request body</param>
        /// <param name="requiresAuth">Whether the endpoint requires authentication</param>
        /// <returns>The deserialized response</returns>
        protected async Task<TResponse?> PutAsync<TRequest, TResponse>(
            string serviceName,
            string endpoint,
            TRequest requestBody,
            bool requiresAuth = true)
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            var response = await client.PutAsJsonAsync(endpoint, requestBody);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
            if (apiResponse == null || !apiResponse.Success)
            {
                throw new InvalidOperationException($"API request failed: {apiResponse?.Message}");
            }

            return apiResponse.Data;
        }

        /// <summary>
        /// Sends a DELETE request to the specified endpoint
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="endpoint">The endpoint to send the request to</param>
        /// <param name="requiresAuth">Whether the endpoint requires authentication</param>
        /// <returns>Whether the request was successful</returns>
        protected async Task<bool> DeleteAsync(string serviceName, string endpoint, bool requiresAuth = true)
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            var response = await client.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();

            return true;
        }

        /// <summary>
        /// Disposes resources used by the test
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    /// <typeparam name="T">The type of data in the response</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets the response data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public string? Code { get; set; }
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Registration request model
    /// </summary>
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}