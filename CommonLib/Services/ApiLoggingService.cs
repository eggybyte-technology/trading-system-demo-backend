using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace CommonLib.Services
{
    /// <summary>
    /// Service for logging API requests and responses
    /// </summary>
    public class ApiLoggingService : IApiLoggingService
    {
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the ApiLoggingService
        /// </summary>
        /// <param name="logger">Logger service dependency</param>
        public ApiLoggingService(ILoggerService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task LogApiRequest(HttpContext context)
        {
            try
            {
                string requestBody = string.Empty;

                // Ensure the request body can be read multiple times
                context.Request.EnableBuffering();

                // Read the request body if it exists
                if (context.Request.Body.CanRead)
                {
                    // Save original position
                    var position = context.Request.Body.Position;

                    using var reader = new StreamReader(
                        context.Request.Body,
                        encoding: Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true);

                    requestBody = await reader.ReadToEndAsync();

                    // Try to format JSON for better readability
                    try
                    {
                        if (!string.IsNullOrEmpty(requestBody) &&
                            context.Request.ContentType?.Contains("application/json") == true)
                        {
                            var jsonObj = JsonSerializer.Deserialize<object>(requestBody);
                            requestBody = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                        }
                    }
                    catch
                    {
                        // Keep original body if JSON parsing fails
                    }

                    // Reset the position to allow subsequent middleware to read the body
                    context.Request.Body.Position = 0;
                }

                // Extract authorization information for debugging
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                _logger.LogDebug($"Authorization Header: {(string.IsNullOrEmpty(authHeader) ? "Not present" : "Bearer [token hidden]")}");

                // Extract and log user information
                var userInfo = ExtractUserInfo(context);

                // Log the API request (in-memory only, no database)
                _logger.LogInformation($"API Request: {context.Request.Method} {context.Request.Path}{context.Request.QueryString} | " +
                                      $"IP: {context.Connection.RemoteIpAddress} | " +
                                      $"User: {userInfo} | " +
                                      $"Body: {(string.IsNullOrEmpty(requestBody) ? "(empty)" : requestBody)}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging API request: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts user information from the HttpContext
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>User information string</returns>
        private string ExtractUserInfo(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return "anonymous";
            }

            try
            {
                // First try to get the user ID - using multiple possible claim types
                var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                             context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                             context.User.FindFirst("UserId")?.Value;

                // Get the username
                var username = context.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ??
                               context.User.FindFirst(ClaimTypes.Name)?.Value;

                // Get the email
                var email = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
                            context.User.FindFirst(ClaimTypes.Email)?.Value;

                // Get roles
                var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var rolesString = roles.Any() ? string.Join(",", roles) : "none";

                // Log user claims for debugging
                _logger.LogDebug($"Authenticated User: ID={userId}, Name={username}, Email={email}, Roles={rolesString}");
                foreach (var claim in context.User.Claims)
                {
                    _logger.LogDebug($"User Claim: {claim.Type}={claim.Value}");
                }

                // Format the user info for logging
                if (!string.IsNullOrEmpty(userId))
                {
                    var userInfoParts = new List<string>();
                    userInfoParts.Add($"ID:{userId}");

                    if (!string.IsNullOrEmpty(username))
                        userInfoParts.Add($"Name:{username}");

                    if (!string.IsNullOrEmpty(email))
                        userInfoParts.Add($"Email:{email}");

                    if (roles.Any())
                        userInfoParts.Add($"Roles:{rolesString}");

                    return string.Join(", ", userInfoParts);
                }

                return "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting user info: {ex.Message}");
                return "error-extracting-user";
            }
        }

        /// <summary>
        /// Logs an API response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="responseBody">Response body</param>
        /// <param name="responseTime">Response time in milliseconds</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task LogApiResponse(HttpContext context, string responseBody, long responseTime)
        {
            try
            {
                // Log the API response (in-memory only, no database)
                _logger.LogInformation($"API Response: {context.Request.Method} {context.Request.Path} | " +
                                      $"Status: {context.Response.StatusCode} | " +
                                      $"Time: {responseTime}ms | " +
                                      $"Body: {(string.IsNullOrEmpty(responseBody) ? "(empty)" : responseBody)}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging API response: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs an API call to another service
        /// </summary>
        /// <typeparam name="T">The return type of the API call</typeparam>
        /// <param name="servicePrefix">The service prefix</param>
        /// <param name="context">HTTP context</param>
        /// <param name="apiCall">The API call to execute</param>
        /// <returns>The result of the API call</returns>
        public async Task<T> LogApiCallAsync<T>(string servicePrefix, HttpContext context, Func<Task<T>> apiCall)
        {
            var userInfo = context != null ? ExtractUserInfo(context) : "system";

            try
            {
                _logger.LogInformation($"External API Call: {servicePrefix} | Initiator: {userInfo}");
                var result = await apiCall();
                _logger.LogInformation($"External API Call Completed: {servicePrefix}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"External API Call Failed: {servicePrefix} | Error: {ex.Message}");
                throw;
            }
        }

        public async Task LogApiCallAsync(string servicePrefix, HttpContext context, Func<Task> apiCall)
        {
            // Log request details
            string requestPath = $"{servicePrefix}:{context.Request.Host.Port}{context.Request.Path}{context.Request.QueryString}";
            string requestMethod = context.Request.Method;
            string requestBody = await GetRequestBodyAsync(context.Request);
            string bodyLog = !string.IsNullOrEmpty(requestBody) ? requestBody : "No body";
            var userInfo = ExtractUserInfo(context);

            _logger.LogInformation($"API Request: {requestMethod} {requestPath} | User: {userInfo}\nBody: {bodyLog}");

            try
            {
                // Execute the API call
                await apiCall();

                // Log response (for void methods)
                _logger.LogInformation($"API Response: {requestMethod} {requestPath}\nResult: No content");
            }
            catch (Exception ex)
            {
                // Log exception
                _logger.LogError($"API Error: {requestMethod} {requestPath}", ex);
                throw;
            }
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            // Ensure the request body can be read multiple times
            request.EnableBuffering();

            // Only read if there is content
            if (request.ContentLength <= 0)
            {
                return string.Empty;
            }

            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true
            );

            var body = await reader.ReadToEndAsync();

            // Reset the position to the beginning for the next reader
            request.Body.Position = 0;

            return body;
        }
    }
}