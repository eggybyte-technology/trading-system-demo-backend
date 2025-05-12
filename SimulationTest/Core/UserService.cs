using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IO;
using Bogus;
using Newtonsoft.Json;
using Spectre.Console;
using CommonLib.Models;
using CommonLib.Models.Identity;
using SimulationTest.Helpers;

namespace SimulationTest.Core
{
    /// <summary>
    /// Service for managing user authentication operations
    /// </summary>
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientFactory _httpClientFactory;
        private readonly Random _random = new Random();
        private readonly List<UserCredential> _users = new List<UserCredential>();
        private readonly System.Text.Json.JsonSerializerOptions _jsonOptions;
        private readonly string _userLogFile;
        private readonly string _responseLogFile;
        private readonly object _userLogLock = new object();

        /// <summary>
        /// Initializes a new instance of the UserService class
        /// </summary>
        /// <param name="identityServiceUrl">URL of the identity service</param>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        public UserService(string identityServiceUrl, HttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            // Get the client from the factory rather than creating a new one
            _httpClient = _httpClientFactory.GetClient("identity");

            _jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Set up log file paths with consistent timestamps
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _userLogFile = Path.Combine("logs", $"detailed_user_log_{timestamp}.txt");
            _responseLogFile = Path.Combine("logs", $"api_responses_users_{timestamp}.txt");

            // Initialize the user log file
            using var logWriter = new StreamWriter(_userLogFile, true);
            logWriter.WriteLine("=== Detailed User Creation Log ===");
            logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine("=================================\n");

            // Initialize the response log file
            using var responseLog = new StreamWriter(_responseLogFile, true);
            responseLog.WriteLine("=== API Response Log - Users ===");
            responseLog.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            responseLog.WriteLine("=================================\n");
        }

        /// <summary>
        /// Initializes a new instance of the UserService class with pre-configured HttpClient
        /// </summary>
        /// <param name="httpClient">Pre-configured HTTP client</param>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="jsonOptions">JSON serialization options</param>
        /// <param name="testFolderPath">Optional path to a test-specific folder for logs</param>
        public UserService(
            HttpClient httpClient,
            HttpClientFactory httpClientFactory,
            System.Text.Json.JsonSerializerOptions jsonOptions,
            string testFolderPath = null)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _jsonOptions = jsonOptions;

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Set up log file paths with consistent timestamps
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // If a test folder is provided, use it for the logs
            if (!string.IsNullOrEmpty(testFolderPath))
            {
                Directory.CreateDirectory(testFolderPath);
                _userLogFile = Path.Combine(testFolderPath, "detailed_user_log.txt");
                _responseLogFile = Path.Combine(testFolderPath, "api_responses_users.txt");
            }
            else
            {
                _userLogFile = Path.Combine("logs", $"detailed_user_log_{timestamp}.txt");
                _responseLogFile = Path.Combine("logs", $"api_responses_users_{timestamp}.txt");
            }

            // Initialize the user log file
            using var logWriter = new StreamWriter(_userLogFile, true);
            logWriter.WriteLine("=== Detailed User Creation Log ===");
            logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine("=================================\n");

            // Initialize the response log file
            using var responseLog = new StreamWriter(_responseLogFile, true);
            responseLog.WriteLine("=== API Response Log - Users ===");
            responseLog.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            responseLog.WriteLine("=================================\n");
        }

        /// <summary>
        /// Registers multiple users asynchronously
        /// </summary>
        /// <param name="count">Number of users to register</param>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <returns>List of registered user credentials</returns>
        public async Task<List<UserCredential>> CreateUsersAsync(int count, bool verbose)
        {
            Console.WriteLine($"Creating {count} users...");

            var tasks = new List<Task<UserCredential>>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(RegisterUserAsync(verbose));
            }

            var users = new List<UserCredential>();

            foreach (var task in tasks)
            {
                try
                {
                    var user = await task;
                    users.Add(user);
                    Console.WriteLine($"Registered new user: {user.Email.Split('@')[0]} ({user.Email})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error registering user: {ex.Message}");
                }
            }

            Console.WriteLine($"Completed creating {users.Count} users out of {count} requested");
            return users;
        }

        /// <summary>
        /// Creates test users for simulation
        /// </summary>
        /// <param name="numUsers">Number of users to create</param>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <returns>List of created user credentials</returns>
        public async Task<List<UserCredential>> CreateTestUsersAsync(int numUsers, bool verbose)
        {
            // Non-interactive implementation that doesn't use AnsiConsole.Status
            Console.WriteLine($"Creating {numUsers} test users...");

            // Create a faker for generating user data
            var faker = new Faker();

            for (int i = 0; i < numUsers; i++)
            {
                Console.WriteLine($"Creating user {i + 1}/{numUsers}");

                // Generate unique username using timestamp + random number
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var randomSuffix = _random.Next(10000, 99999);
                var username = $"user_{timestamp}_{randomSuffix}";
                var email = $"{username}@trading-simulation.test";
                var password = "Test123!";

                try
                {
                    // Create register request
                    var registerRequest = new RegisterRequest
                    {
                        Username = username,
                        Email = email,
                        Password = password
                    };

                    await LogUserActionAsync($"Registering user: {email}", verbose);

                    // Send registration request
                    var registerResponse = await _httpClient.PostAsJsonAsync("/auth/register", registerRequest);
                    string content = await registerResponse.Content.ReadAsStringAsync();

                    // Log the full response
                    await LogResponseAsync($"Register Response:\nStatus: {registerResponse.StatusCode}\nContent: {content}");

                    if (registerResponse.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Try parsing directly into RegisterResponse first
                            var directResponse = JsonConvert.DeserializeObject<RegisterResponse>(content);
                            if (directResponse != null && !string.IsNullOrEmpty(directResponse.UserId))
                            {
                                var user = new UserCredential
                                {
                                    UserId = directResponse.UserId,
                                    Email = email,
                                    Token = directResponse.Token
                                };

                                _users.Add(user);

                                // Setup user-specific HTTP clients with JWT token
                                _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);

                                await LogUserActionAsync($"User registered successfully: {user.Email}, UserId: {user.UserId}", verbose);
                                await LogUserActionAsync($"JWT Token: {user.Token.Substring(0, 20)}...", verbose);

                                if (verbose)
                                    Console.WriteLine($"Registered new user: {username} ({email})");

                                continue; // Skip to next iteration
                            }
                        }
                        catch (Exception ex)
                        {
                            await LogUserActionAsync($"Error parsing direct response: {ex.Message}", verbose);
                        }

                        // Try parsing with wrapper if direct parsing failed
                        try
                        {
                            var response = JsonConvert.DeserializeObject<ApiResponse<RegisterResponse>>(content);

                            if (response?.Data != null)
                            {
                                var user = new UserCredential
                                {
                                    UserId = response.Data.UserId,
                                    Email = email,
                                    Token = response.Data.Token
                                };

                                _users.Add(user);

                                // Setup user-specific HTTP clients with JWT token
                                _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);

                                await LogUserActionAsync($"User registered successfully: {user.Email}, UserId: {user.UserId}", verbose);
                                await LogUserActionAsync($"JWT Token: {user.Token.Substring(0, 20)}...", verbose);

                                if (verbose)
                                    Console.WriteLine($"Registered new user: {username} ({email})");
                            }
                            else
                            {
                                // Last attempt: try to manually parse the JSON
                                try
                                {
                                    var jsonData = JsonConvert.DeserializeObject<dynamic>(content);
                                    string? userId = null;
                                    string? token = null;

                                    // Try to look for common patterns
                                    if (jsonData != null)
                                    {
                                        if (jsonData.userId != null)
                                            userId = (string)jsonData.userId;
                                        else if (jsonData.UserId != null)
                                            userId = (string)jsonData.UserId;
                                        else if (jsonData.data != null && jsonData.data.userId != null)
                                            userId = (string)jsonData.data.userId;
                                        else if (jsonData.data != null && jsonData.data.UserId != null)
                                            userId = (string)jsonData.data.UserId;

                                        if (jsonData.token != null)
                                            token = (string)jsonData.token;
                                        else if (jsonData.Token != null)
                                            token = (string)jsonData.Token;
                                        else if (jsonData.data != null && jsonData.data.token != null)
                                            token = (string)jsonData.data.token;
                                        else if (jsonData.data != null && jsonData.data.Token != null)
                                            token = (string)jsonData.data.Token;

                                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token))
                                        {
                                            var user = new UserCredential
                                            {
                                                UserId = userId,
                                                Email = email,
                                                Token = token
                                            };

                                            _users.Add(user);

                                            // Setup user-specific HTTP clients with JWT token
                                            _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);

                                            await LogUserActionAsync($"User registered with manual parsing: {user.Email}, UserId: {user.UserId}", verbose);

                                            if (verbose)
                                                Console.WriteLine($"Registered new user (manual): {username} ({email})");
                                        }
                                    }
                                }
                                catch (Exception parseEx)
                                {
                                    await LogUserActionAsync($"Final attempt to parse response failed: {parseEx.Message}", verbose);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await LogUserActionAsync($"Error parsing response: {ex.Message}", verbose);
                        }
                    }
                    else
                    {
                        await LogUserActionAsync($"Registration failed: {content}", verbose);
                        Console.WriteLine($"Failed to register user: {content}");
                    }
                }
                catch (Exception ex)
                {
                    await LogUserActionAsync($"Exception during registration: {ex.Message}", verbose);
                    Console.WriteLine($"Error registering user: {ex.Message}");
                }
            }

            Console.WriteLine($"Created {_users.Count} test users out of {numUsers} requested");
            await LogUserActionAsync($"Completed creating {_users.Count} users out of {numUsers} requested", verbose);

            return _users;
        }

        /// <summary>
        /// Register a single user asynchronously
        /// </summary>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <returns>Registered user credential</returns>
        private async Task<UserCredential> RegisterUserAsync(bool verbose)
        {
            // Create a faker for generating user data
            var faker = new Faker();

            // Generate unique username using timestamp + random number
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var randomSuffix = _random.Next(10000, 99999);
            var username = $"user_{timestamp}_{randomSuffix}";
            var email = $"{username}@trading-simulation.test";
            var password = "Test123!";

            // Log user creation attempt
            await LogUserActionAsync($"Creating user: {username} ({email})", verbose);

            // Register a new user
            var registerRequest = new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = password,
                Phone = faker.Phone.PhoneNumber()
            };

            // Log the register request
            await LogUserActionAsync($"Registration request: {System.Text.Json.JsonSerializer.Serialize(registerRequest, _jsonOptions)}", verbose);

            var registerResponse = await _httpClient.PostAsJsonAsync("/auth/register", registerRequest);
            var content = await registerResponse.Content.ReadAsStringAsync();
            await LogUserActionAsync($"Raw registration response: {content}", verbose);

            if (!registerResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to register user: {content}");
            }

            try
            {
                // Try parsing directly into RegisterResponse first
                var directResponse = JsonConvert.DeserializeObject<RegisterResponse>(content);
                if (directResponse != null && !string.IsNullOrEmpty(directResponse.UserId))
                {
                    var user = new UserCredential
                    {
                        UserId = directResponse.UserId,
                        Email = email,
                        Token = directResponse.Token
                    };

                    // Setup user-specific HTTP clients with JWT token
                    _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);

                    await LogUserActionAsync($"User registered successfully: {user.Email}, UserId: {user.UserId}", verbose);
                    await LogUserActionAsync($"JWT Token: {user.Token.Substring(0, 20)}...", verbose);

                    return user;
                }
            }
            catch (Exception ex)
            {
                await LogUserActionAsync($"Error parsing direct response: {ex.Message}", verbose);
            }

            // Try parsing with wrapper if direct parsing failed
            try
            {
                var response = JsonConvert.DeserializeObject<ApiResponse<RegisterResponse>>(content);

                if (response?.Data != null)
                {
                    var user = new UserCredential
                    {
                        UserId = response.Data.UserId,
                        Email = email,
                        Token = response.Data.Token
                    };

                    // Setup user-specific HTTP clients with JWT token
                    _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);

                    await LogUserActionAsync($"User registered successfully: {user.Email}, UserId: {user.UserId}", verbose);
                    await LogUserActionAsync($"JWT Token: {user.Token.Substring(0, 20)}...", verbose);

                    return user;
                }
                else
                {
                    throw new Exception($"Registration successful but data is null or invalid in wrapped response: {content}");
                }
            }
            catch (Exception ex)
            {
                await LogUserActionAsync($"Error parsing wrapped response: {ex.Message}", verbose);

                // Last attempt: try to extract directly using string parsing if JSON deserialization fails
                try
                {
                    // Look for the userId and token in the response
                    if (content.Contains("userId") && content.Contains("token"))
                    {
                        // Extract userId using simple string parsing
                        int userIdStart = content.IndexOf("userId") + 9;
                        int userIdEnd = content.IndexOf("\"", userIdStart);
                        string userId = content.Substring(userIdStart, userIdEnd - userIdStart);

                        // Extract token
                        int tokenStart = content.IndexOf("token") + 8;
                        int tokenEnd = content.IndexOf("\"", tokenStart);
                        string token = content.Substring(tokenStart, tokenEnd - tokenStart);

                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token))
                        {
                            var user = new UserCredential
                            {
                                UserId = userId,
                                Email = email,
                                Token = token
                            };

                            // Setup user-specific HTTP clients with JWT token
                            _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);

                            await LogUserActionAsync($"User registered with manual parsing: {user.Email}, UserId: {user.UserId}", verbose);

                            return user;
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    await LogUserActionAsync($"Final attempt to parse response failed: {parseEx.Message}", verbose);
                }

                throw new Exception($"Failed to parse registration response: {content}");
            }
        }

        /// <summary>
        /// Log response to a response log file
        /// </summary>
        /// <param name="message">The message to log</param>
        private async Task LogResponseAsync(string message)
        {
            lock (_userLogLock)
            {
                using var responseLog = new StreamWriter(_responseLogFile, true);
                responseLog.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
                responseLog.WriteLine("-------------------\n");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Log user action to a detailed log file
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="verbose">Whether to also display in console</param>
        private Task LogUserActionAsync(string message, bool verbose)
        {
            lock (_userLogLock)
            {
                using var logWriter = new StreamWriter(_userLogFile, true);
                logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }

            if (verbose)
                Console.WriteLine($"{message}");

            return Task.CompletedTask;
        }
    }
}