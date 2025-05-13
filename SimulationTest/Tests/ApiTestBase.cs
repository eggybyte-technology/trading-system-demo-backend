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
using System.Linq;

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
        // Changed to static to share authentication state between test classes
        protected static SecurityToken? _token;
        protected static string? _userId;

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
                { "identity", _configuration["Services:IdentityHost"] ?? "http://identity.trading-system.local" },
                { "trading", _configuration["Services:TradingHost"] ?? "http://trading.trading-system.local" },
                { "market-data", _configuration["Services:MarketDataHost"] ?? "http://market-data.trading-system.local" },
                { "account", _configuration["Services:AccountHost"] ?? "http://account.trading-system.local" },
                { "risk", _configuration["Services:RiskHost"] ?? "http://risk.trading-system.local" },
                { "notification", _configuration["Services:NotificationHost"] ?? "http://notification.trading-system.local" },
                { "match-making", _configuration["Services:MatchMakingHost"] ?? "http://match-making.trading-system.local" }
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
            try
            {
                // Get the identity service client
                var client = _httpClientFactory.GetClient("identity");

                // Create login request
                var loginRequest = new LoginRequest
                {
                    Email = username,
                    Password = password
                };

                // Send login request
                var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Authentication failed: {errorContent}");
                    return null;
                }

                // Read and deserialize response
                var content = await response.Content.ReadAsStringAsync();

                try
                {
                    // Try parsing as direct LoginResponse first
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        var token = new SecurityToken
                        {
                            Token = loginResponse.Token,
                            UserId = MongoDB.Bson.ObjectId.Parse(loginResponse.UserId)
                        };

                        // Store token for reuse
                        _token = token;
                        _userId = loginResponse.UserId;

                        // Update HttpClientFactory with token
                        _httpClientFactory.SetUserAuthToken(_userId, _token.Token);

                        return token;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing direct response: {ex.Message}");

                    // If direct parsing fails, try with ApiResponse wrapper
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);
                        if (apiResponse?.Data != null && !string.IsNullOrEmpty(apiResponse.Data.Token))
                        {
                            var token = new SecurityToken
                            {
                                Token = apiResponse.Data.Token,
                                UserId = MongoDB.Bson.ObjectId.Parse(apiResponse.Data.UserId)
                            };

                            // Store token for reuse
                            _token = token;
                            _userId = apiResponse.Data.UserId;

                            // Update HttpClientFactory with token
                            _httpClientFactory.SetUserAuthToken(_userId, _token.Token);

                            return token;
                        }
                    }
                    catch (Exception nestedEx)
                    {
                        Console.WriteLine($"Error parsing token response: {nestedEx.Message}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication exception: {ex.Message}");
                return null;
            }
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
            // Check if we have a token in this instance
            if (_token == null)
            {
                // Log detailed diagnostic information
                Console.WriteLine("Authentication token is null. Checking static authentication state:");
                Console.WriteLine($"- User ID: {_userId ?? "null"}");

                // Check if token is stored in HttpClientFactory
                if (!string.IsNullOrEmpty(_userId))
                {
                    var client = _httpClientFactory.GetUserClient(_userId, "identity");
                    var authHeader = client.DefaultRequestHeaders.Authorization;
                    Console.WriteLine($"- Authorization header: {(authHeader != null ? "present" : "missing")}");
                    if (authHeader != null)
                    {
                        Console.WriteLine($"- Auth scheme: {authHeader.Scheme}");
                        Console.WriteLine($"- Token present: {!string.IsNullOrEmpty(authHeader.Parameter)}");
                        if (!string.IsNullOrEmpty(authHeader.Parameter))
                        {
                            // We have a token in the client, try to use it
                            _token = new SecurityToken { Token = authHeader.Parameter };
                            if (!string.IsNullOrEmpty(_userId) && ObjectId.TryParse(_userId, out var objectId))
                            {
                                _token.UserId = objectId;
                            }
                            Console.WriteLine("- Retrieved token from HttpClientFactory");
                            return;
                        }
                    }
                }

                // No token available - suggest running identity tests first
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
        protected async Task<T> GetAsync<T>(string serviceName, string endpoint, bool requiresAuth = true) where T : class, new()
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            var response = await client.GetAsync(endpoint);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Log the detailed response for easier debugging
            Console.WriteLine($"GET {endpoint} response status: {response.StatusCode}");

            // Only log full response content in verbose mode to avoid cluttering the console
            bool verbose = bool.TryParse(_configuration["TestSettings:Verbose"], out var v) && v;
            if (verbose)
            {
                // Truncate very long responses for display
                string displayContent = responseContent;
                if (displayContent.Length > 1000)
                {
                    displayContent = displayContent.Substring(0, 997) + "...";
                }
                Console.WriteLine($"GET {endpoint} response content: {displayContent}");
            }
            else
            {
                // Always show full response preview instead of truncating to 100 chars
                Console.WriteLine($"GET {endpoint} response preview: {responseContent}");
            }

            try
            {
                response.EnsureSuccessStatusCode();

                // Parse the response using our simplified approach
                return await ProcessApiResponseAsync<T>(response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error: {ex.Message}");
                throw;
            }
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
        protected async Task<TResponse> PostAsync<TRequest, TResponse>(
            string serviceName,
            string endpoint,
            TRequest requestBody,
            bool requiresAuth = true) where TResponse : class, new()
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            // Check if verbose mode is enabled
            bool verbose = bool.TryParse(_configuration["TestSettings:Verbose"], out var v) && v;

            // Log the request body for easier debugging
            if (verbose)
            {
                Console.WriteLine($"POST {endpoint} request: {JsonSerializer.Serialize(requestBody, _jsonOptions)}");
            }

            var response = await client.PostAsJsonAsync(endpoint, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Log the detailed response for easier debugging
            Console.WriteLine($"POST {endpoint} response status: {response.StatusCode}");

            // Only log full response content in verbose mode
            if (verbose)
            {
                // Truncate very long responses for display
                string displayContent = responseContent;
                if (displayContent.Length > 1000)
                {
                    displayContent = displayContent.Substring(0, 997) + "...";
                }
                Console.WriteLine($"POST {endpoint} response content: {displayContent}");
            }
            else
            {
                // Always show full response preview instead of truncating to 100 chars
                Console.WriteLine($"POST {endpoint} response preview: {responseContent}");
            }

            try
            {
                response.EnsureSuccessStatusCode();

                // Parse the response using our simplified approach
                return await ProcessApiResponseAsync<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error: {ex.Message}");
                throw;
            }
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
        protected async Task<TResponse> PutAsync<TRequest, TResponse>(
            string serviceName,
            string endpoint,
            TRequest requestBody,
            bool requiresAuth = true) where TResponse : class, new()
        {
            if (requiresAuth)
                await EnsureAuthenticatedAsync();

            var client = requiresAuth
                ? CreateAuthorizedClient(serviceName)
                : _httpClientFactory.GetClient(serviceName);

            // Check if verbose mode is enabled
            bool verbose = bool.TryParse(_configuration["TestSettings:Verbose"], out var v) && v;

            // Log the request body for easier debugging
            if (verbose)
            {
                Console.WriteLine($"PUT {endpoint} request: {JsonSerializer.Serialize(requestBody, _jsonOptions)}");
            }

            var response = await client.PutAsJsonAsync(endpoint, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Log the detailed response for easier debugging
            Console.WriteLine($"PUT {endpoint} response status: {response.StatusCode}");

            // Only log full response content in verbose mode
            if (verbose)
            {
                // Truncate very long responses for display
                string displayContent = responseContent;
                if (displayContent.Length > 1000)
                {
                    displayContent = displayContent.Substring(0, 997) + "...";
                }
                Console.WriteLine($"PUT {endpoint} response content: {displayContent}");
            }
            else
            {
                // Always show full response preview instead of truncating to 100 chars
                Console.WriteLine($"PUT {endpoint} response preview: {responseContent}");
            }

            try
            {
                response.EnsureSuccessStatusCode();

                // Parse the response using our simplified approach
                return await ProcessApiResponseAsync<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error: {ex.Message}");
                throw;
            }
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
        /// Processes and deserializes an API response into the requested type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="response">The HTTP response to process</param>
        /// <returns>The deserialized object</returns>
        private async Task<T> ProcessApiResponseAsync<T>(HttpResponseMessage response) where T : class, new()
        {
            string responseContent = await response.Content.ReadAsStringAsync();

            // First, try to parse as ApiResponse<T>
            try
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                if (apiResponse != null && (apiResponse.Success || apiResponse.Data != null))
                {
                    return apiResponse.Data;
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Could not parse as ApiResponse<T>: {jsonEx.Message}");
                // Continue to try other formats
            }

            // Try to parse as direct T object
            try
            {
                var directObject = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                if (directObject != null)
                {
                    return directObject;
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Could not parse as direct object: {jsonEx.Message}");
                // Continue to try other formats
            }

            // Try to handle JSON arrays for collection types
            bool isCollection = typeof(T).IsGenericType &&
                (typeof(T).GetGenericTypeDefinition() == typeof(List<>) ||
                 typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 typeof(T).GetGenericTypeDefinition() == typeof(ICollection<>));

            if (isCollection && responseContent.TrimStart().StartsWith("["))
            {
                try
                {
                    var arrayResponse = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                    if (arrayResponse != null)
                    {
                        return arrayResponse;
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"Could not parse as collection: {jsonEx.Message}");
                }
            }

            // Try to handle custom wrapper formats
            try
            {
                // Check common container formats like {"balances":[]} or {"items":[]}
                // This is a simplified approach - the full set of checks is in the original code
                var expectedProperties = typeof(T).GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();
                var commonContainerNames = new[] { "balances", "markets", "symbols", "items", "orders", "trades", "notifications", "transactions" };

                // Try to create a dynamic object to inspect the response structure
                var jsonDocument = JsonDocument.Parse(responseContent);
                var rootElement = jsonDocument.RootElement;

                if (rootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        // If we find a property that corresponds to a collection in our expected type,
                        // or matches a common container name, try direct deserialization
                        if (commonContainerNames.Contains(property.Name.ToLowerInvariant()) ||
                            expectedProperties.Contains(property.Name.ToLowerInvariant()))
                        {
                            // Try direct deserialization again
                            var containerObject = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                            if (containerObject != null)
                            {
                                return containerObject;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error examining response structure: {ex.Message}");
            }

            // If all else fails, create a new instance as a last resort
            Console.WriteLine("All parsing attempts failed, creating empty instance");
            return new T();
        }

        /// <summary>
        /// Disposes resources used by the test
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    // Using CommonLib.Models.ApiResponse<T>, CommonLib.Models.Identity.LoginRequest, and CommonLib.Models.Identity.RegisterRequest directly
}