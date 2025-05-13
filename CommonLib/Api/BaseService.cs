using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    /// <summary>
    /// Base service class with common functionality for all API service clients
    /// </summary>
    public abstract class BaseService
    {
        protected readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger? _logger;
        protected readonly string _serviceName;

        protected BaseService(IConfiguration configuration, string serviceConfigKey, string defaultUrl, ILogger? logger = null)
        {
            _configuration = configuration;
            var baseUrl = configuration[$"ServiceUrls:{serviceConfigKey}"] ?? defaultUrl;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _logger = logger;
            _serviceName = serviceConfigKey;
        }

        /// <summary>
        /// Sets the authentication header with the provided token
        /// </summary>
        /// <param name="token">JWT token for authorization</param>
        protected void SetAuthHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Builds a query string from the provided parameters
        /// </summary>
        /// <param name="parameters">Dictionary of query parameters</param>
        /// <returns>Formatted query string</returns>
        protected string BuildQueryString(Dictionary<string, string?> parameters)
        {
            var validParams = parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");

            return string.Join("&", validParams);
        }

        /// <summary>
        /// Sends a GET request to the specified endpoint with detailed logging
        /// </summary>
        protected async Task<T> GetAsync<T>(string endpoint, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                SetAuthHeader(token);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// Sends a POST request to the specified endpoint with detailed logging
        /// </summary>
        protected async Task<T> PostAsync<T, TRequest>(string endpoint, TRequest content, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                SetAuthHeader(token);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(content, options: _jsonOptions)
            };

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// Sends a PUT request to the specified endpoint with detailed logging
        /// </summary>
        protected async Task<T> PutAsync<T, TRequest>(string endpoint, TRequest content, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                SetAuthHeader(token);
            }

            var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
            {
                Content = JsonContent.Create(content, options: _jsonOptions)
            };

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// Sends a DELETE request to the specified endpoint with detailed logging
        /// </summary>
        protected async Task<T> DeleteAsync<T>(string endpoint, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                SetAuthHeader(token);
            }

            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// Common method to send HTTP requests with logging and error handling
        /// </summary>
        private async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            LogHttpRequest(request);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                var content = await response.Content.ReadAsStringAsync();

                LogHttpResponse(response, content, stopwatch.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"HTTP request failed: {(int)response.StatusCode} {response.StatusCode}\n" +
                        $"URL: {request.RequestUri}\n" +
                        $"Response: {content}"
                    );
                }

                var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);

                if (result == null)
                {
                    throw new JsonException("Failed to deserialize response");
                }

                if (!result.Success)
                {
                    throw new ApplicationException(
                        $"API returned error: {result.Message} (Code: {result.Code})"
                    );
                }

                return result.Data != null
                    ? result.Data
                    : throw new NullReferenceException("API returned null data");
            }
            catch (Exception ex) when (
                ex is not HttpRequestException &&
                ex is not JsonException &&
                ex is not ApplicationException &&
                ex is not NullReferenceException)
            {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }

                _logger?.LogError("Exception in HTTP request: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Logs HTTP request details for debugging
        /// </summary>
        private void LogHttpRequest(HttpRequestMessage request)
        {
            if (_logger == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[HTTP Request] - {_serviceName}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"URL: {request.RequestUri}");

            if (request.Headers != null && request.Headers.Any())
            {
                sb.AppendLine("Headers:");
                foreach (var header in request.Headers)
                {
                    string headerValue = header.Key == "Authorization"
                        ? "Bearer [token-hidden]"
                        : string.Join(", ", header.Value);
                    sb.AppendLine($"  {header.Key}: {headerValue}");
                }
            }

            // Log request body if it exists
            if (request.Content != null)
            {
                try
                {
                    string content = request.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(content))
                    {
                        // Try to format if it's JSON
                        if (request.Content.Headers.ContentType?.MediaType == "application/json")
                        {
                            try
                            {
                                var jsonObj = JsonSerializer.Deserialize<object>(content);
                                content = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                            }
                            catch
                            {
                                // Keep original content if JSON parsing fails
                            }
                        }

                        // Limit content length for large bodies
                        const int maxContentLength = 4000;
                        string truncatedContent = content.Length > maxContentLength
                            ? content.Substring(0, maxContentLength) + "... [truncated]"
                            : content;

                        sb.AppendLine("Body:");
                        sb.AppendLine(truncatedContent);
                    }
                    else
                    {
                        sb.AppendLine("Body: (empty)");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Body: (Error reading body: {ex.Message})");
                }
            }
            else
            {
                sb.AppendLine("Body: (none)");
            }

            _logger.LogDebug(sb.ToString());
        }

        /// <summary>
        /// Logs HTTP response details for debugging
        /// </summary>
        private void LogHttpResponse(HttpResponseMessage response, string content, long elapsedMs)
        {
            if (_logger == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[HTTP Response] - {_serviceName} - {elapsedMs}ms");
            sb.AppendLine($"Status: {(int)response.StatusCode} {response.StatusCode}");

            if (response.Headers != null && response.Headers.Any())
            {
                sb.AppendLine("Headers:");
                foreach (var header in response.Headers)
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            // Limit response content to avoid excessive logging
            const int maxContentLength = 4000;
            string truncatedContent = content.Length > maxContentLength
                ? content.Substring(0, maxContentLength) + "... [truncated]"
                : content;

            sb.AppendLine("Content:");
            sb.AppendLine(truncatedContent);

            _logger.LogDebug(sb.ToString());
        }
    }
}