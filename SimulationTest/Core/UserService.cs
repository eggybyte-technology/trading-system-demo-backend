using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
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
        private readonly string _logFolderPath;

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
            _logFolderPath = testFolderPath;

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
        /// Creates test users for the stress test
        /// </summary>
        public async Task<List<LoginResponse>> CreateTestUsersAsync(int userCount, bool verbose = false, IProgress<TestProgress> progress = null)
        {
            List<LoginResponse> users = new List<LoginResponse>();
            int existingUsers = 0;
            int failedRegistrations = 0;
            int successfulRegistrations = 0;

            for (int i = 0; i < userCount; i++)
            {
                string username = $"testuser_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                string email = $"{username}@example.com";
                string password = $"Password1!{Guid.NewGuid().ToString("N").Substring(0, 8)}";

                try
                {
                    var registerRequest = new RegisterRequest
                    {
                        Username = username,
                        Email = email,
                        Password = password
                    };

                    if (verbose)
                    {
                        Console.WriteLine($"Registering user {i + 1}/{userCount}: {username}");
                    }

                    var response = await _httpClient.PostAsJsonAsync("/auth/register", registerRequest);
                    string content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (content.Contains("already exists"))
                        {
                            if (verbose)
                            {
                                Console.WriteLine($"User already exists, trying to login");
                            }
                            existingUsers++;

                            // Try to login
                            var loginResponse = await LoginAsync(email, password);
                            if (loginResponse != null)
                            {
                                users.Add(loginResponse);
                                successfulRegistrations++;
                            }
                            else
                            {
                                failedRegistrations++;
                                if (verbose)
                                {
                                    Console.WriteLine($"Failed to login existing user: {email}");
                                }
                            }
                        }
                        else
                        {
                            failedRegistrations++;
                            if (verbose)
                            {
                                Console.WriteLine($"Failed to register user: {content}");
                            }
                        }
                    }
                    else
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"Successfully registered: {username}");
                        }

                        // Try to parse the response
                        try
                        {
                            var jsonOptions = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            var apiResponse = JsonSerializer.Deserialize<ApiResponse<RegisterResponse>>(content, jsonOptions);
                            if (apiResponse?.Data != null)
                            {
                                var registeredUser = new LoginResponse
                                {
                                    Username = apiResponse.Data.Username,
                                    UserId = apiResponse.Data.UserId,
                                    Token = apiResponse.Data.Token,
                                    RefreshToken = apiResponse.Data.RefreshToken,
                                    Expiration = apiResponse.Data.Expiration
                                };

                                users.Add(registeredUser);
                                successfulRegistrations++;

                                // Configure the HttpClientFactory with the user token
                                _httpClientFactory.SetUserAuthToken(registeredUser.UserId, registeredUser.Token);
                            }
                            else
                            {
                                var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(content, jsonOptions);
                                if (registerResponse != null)
                                {
                                    var registeredUser = new LoginResponse
                                    {
                                        Username = registerResponse.Username,
                                        UserId = registerResponse.UserId,
                                        Token = registerResponse.Token,
                                        RefreshToken = registerResponse.RefreshToken,
                                        Expiration = registerResponse.Expiration
                                    };

                                    users.Add(registeredUser);
                                    successfulRegistrations++;

                                    // Configure the HttpClientFactory with the user token
                                    _httpClientFactory.SetUserAuthToken(registeredUser.UserId, registeredUser.Token);
                                }
                                else
                                {
                                    failedRegistrations++;
                                    if (verbose)
                                    {
                                        Console.WriteLine($"Failed to parse response: {content}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failedRegistrations++;
                            if (verbose)
                            {
                                Console.WriteLine($"Error parsing registration response: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedRegistrations++;
                    if (verbose)
                    {
                        Console.WriteLine($"Exception registering user: {ex.Message}");
                    }
                }

                // 报告进度更新
                if (progress != null)
                {
                    double successRate = (i + 1) > 0 ? (double)successfulRegistrations / (i + 1) * 100 : 0;

                    progress.Report(new TestProgress
                    {
                        Message = "Registering users",
                        Percentage = (int)(100.0 * (i + 1) / userCount),
                        Completed = i + 1,
                        Total = userCount,
                        Passed = successfulRegistrations,
                        Failed = failedRegistrations,
                        Skipped = existingUsers,
                        SuccessRate = successRate,
                        LogMessage = $"Registered user {i + 1}/{userCount}: Success={successfulRegistrations}, Failed={failedRegistrations}, Existing={existingUsers}",
                        Timestamp = DateTime.Now
                    });
                }

                // Small delay to avoid overwhelming the API
                await Task.Delay(50);
            }

            if (verbose || users.Count < userCount)
            {
                Console.WriteLine($"User creation results: Total={userCount}, Success={successfulRegistrations}, Failed={failedRegistrations}, ExistingUsers={existingUsers}");
            }

            // Save the credentials to a file for future reference/debugging
            if (!string.IsNullOrEmpty(_logFolderPath))
            {
                SaveUsersToFile(users, Path.Combine(_logFolderPath, "users.json"));
            }

            return users;
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

        /// <summary>
        /// Attempts to login with the given credentials
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="password">User password</param>
        /// <returns>Login response if successful, null otherwise</returns>
        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Email = email,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync("/auth/login", loginRequest);
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Login failed: {content}");
                    return null;
                }

                try
                {
                    var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);
                    if (apiResponse?.Data != null)
                    {
                        // Configure the HttpClientFactory with the user token
                        _httpClientFactory.SetUserAuthToken(apiResponse.Data.UserId, apiResponse.Data.Token);
                        return apiResponse.Data;
                    }

                    var directResponse = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
                    if (directResponse != null)
                    {
                        // Configure the HttpClientFactory with the user token
                        _httpClientFactory.SetUserAuthToken(directResponse.UserId, directResponse.Token);
                        return directResponse;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing login response: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during login: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Saves user credentials to a JSON file
        /// </summary>
        /// <param name="users">List of user credentials</param>
        /// <param name="filePath">Path to save the file</param>
        private void SaveUsersToFile(List<LoginResponse> users, string filePath)
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(users, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving users to file: {ex.Message}");
            }
        }
    }
}