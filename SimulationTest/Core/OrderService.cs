using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using CommonLib.Models;
using CommonLib.Models.Trading;
using CommonLib.Models.Identity;
using SimulationTest.Strategies;
using SimulationTest.Helpers;

namespace SimulationTest.Core
{
    /// <summary>
    /// Service for submitting and processing orders
    /// </summary>
    public class OrderService
    {
        private readonly HttpClientFactory _httpClientFactory;
        private readonly int _totalUsers;
        private readonly int _ordersPerUser;
        private readonly Random _random = new Random();
        private readonly ConcurrentBag<long> _latencies = new ConcurrentBag<long>();
        private readonly List<string> _symbolList;
        private readonly Dictionary<string, (decimal Min, decimal Max)> _priceRanges;
        private readonly int _baseDelay;
        private readonly (int Min, int Max) _requestDelay;
        private readonly string _orderLogFile;
        private readonly string _responseLogFile;
        private readonly object _logLock = new object();

        private int _successCount;
        private int _failureCount;
        private int _totalOperations;

        /// <summary>
        /// Initializes a new instance of the OrderService class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="totalUsers">Total number of users in the simulation</param>
        /// <param name="ordersPerUser">Number of orders per user</param>
        /// <param name="symbolList">List of trading symbols</param>
        /// <param name="priceRanges">Dictionary of price ranges for each symbol</param>
        /// <param name="baseDelay">Base delay between requests in milliseconds</param>
        /// <param name="requestDelay">Min and max delay between requests</param>
        public OrderService(
            HttpClientFactory httpClientFactory,
            int totalUsers,
            int ordersPerUser,
            List<string> symbolList,
            Dictionary<string, (decimal Min, decimal Max)> priceRanges,
            int baseDelay,
            (int Min, int Max) requestDelay)
        {
            _httpClientFactory = httpClientFactory;
            _totalUsers = totalUsers;
            _ordersPerUser = ordersPerUser;
            _symbolList = symbolList;
            _priceRanges = priceRanges;
            _baseDelay = baseDelay;
            _requestDelay = requestDelay;

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Set up log file paths
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _orderLogFile = Path.Combine("logs", $"detailed_order_log_{timestamp}.txt");
            _responseLogFile = Path.Combine("logs", $"api_responses_{timestamp}.txt");

            // Initialize the order log file
            using var logWriter = new StreamWriter(_orderLogFile, true);
            logWriter.WriteLine("=== Detailed Order Creation Log ===");
            logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine("=================================\n");

            // Initialize the response log file
            using var responseLog = new StreamWriter(_responseLogFile, true);
            responseLog.WriteLine("=== API Response Log ===");
            responseLog.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            responseLog.WriteLine("=================================\n");
        }

        /// <summary>
        /// Gets the total number of successful operations
        /// </summary>
        public int SuccessCount => _successCount;

        /// <summary>
        /// Gets the total number of failed operations
        /// </summary>
        public int FailureCount => _failureCount;

        /// <summary>
        /// Gets the total number of operations
        /// </summary>
        public int TotalOperations => _totalOperations;

        /// <summary>
        /// Gets the latencies recorded for all operations
        /// </summary>
        public ConcurrentBag<long> Latencies => _latencies;

        /// <summary>
        /// Submits random orders for a user
        /// </summary>
        /// <param name="user">The user credentials</param>
        /// <param name="numOrders">Number of orders to submit</param>
        /// <param name="ctx">Status context for progress reporting</param>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <param name="consoleOutput">Whether to output to console</param>
        public async Task SubmitRandomOrdersAsync(UserCredential user, int numOrders, StatusContext? ctx, bool verbose, bool consoleOutput = true)
        {
            int userOrdersSubmitted = 0;
            int userOrdersSucceeded = 0;

            await LogOrderActionAsync($"Starting random orders for user {user.Email}", verbose, consoleOutput);

            if (consoleOutput)
            {
                AnsiConsole.MarkupLine($"[blue]Starting random orders for {user.Email}[/]");
            }

            for (int i = 0; i < numOrders; i++)
            {
                var orderNumber = Interlocked.Increment(ref _totalOperations);
                userOrdersSubmitted++;

                try
                {
                    // Get the user-specific HTTP client for trading service
                    var httpClient = _httpClientFactory.GetUserClient(user.UserId, "trading");

                    // Check if authorization header is set correctly
                    var authHeader = httpClient.DefaultRequestHeaders.Authorization;
                    if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
                    {
                        // Re-apply the token if it's missing
                        _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);
                        await LogOrderActionAsync($"Re-applying token for user {user.Email} before order submission", verbose, consoleOutput);
                    }

                    // Generate random order
                    var symbol = _symbolList[_random.Next(_symbolList.Count)];
                    var orderRequest = SimulationStrategies.GenerateRandomOrder(symbol, _priceRanges);

                    // Log detailed order information
                    await LogOrderActionAsync($"Creating order #{orderNumber} for user {user.Email} - Symbol: {orderRequest.Symbol}, Side: {orderRequest.Side}, Type: {orderRequest.Type}, Price: {orderRequest.Price}, Quantity: {orderRequest.Quantity}", verbose, consoleOutput);

                    // Log the order being submitted - fix markup issue by not trying to style the order type
                    if (consoleOutput)
                    {
                        AnsiConsole.MarkupLine($"[grey]Submitting order {orderNumber}: {orderRequest.Side} {orderRequest.Quantity} {orderRequest.Symbol} @ {orderRequest.Price} ({orderRequest.Type})[/]");
                    }

                    // Submit the order and measure latency
                    var stopwatch = Stopwatch.StartNew();
                    var response = await httpClient.PostAsJsonAsync("/order", orderRequest);
                    stopwatch.Stop();
                    _latencies.Add(stopwatch.ElapsedMilliseconds);

                    // Process response
                    await ProcessResponseAsync(response, orderRequest, user, verbose, consoleOutput);

                    // Update counters based on success/failure
                    if (response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref _successCount);
                        userOrdersSucceeded++;

                        // Update progress if available
                        if (ctx != null && consoleOutput)
                        {
                            ctx.Status($"Processing order {orderNumber}/{numOrders}");
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref _failureCount);
                    }
                }
                catch (Exception ex)
                {
                    await LogOrderActionAsync($"Error submitting order: {ex.Message}", verbose, consoleOutput);

                    if (consoleOutput)
                    {
                        AnsiConsole.MarkupLine($"[red]Error submitting order: {ex.Message}[/]");
                    }

                    Interlocked.Increment(ref _failureCount);
                }
                finally
                {
                    // Add some delay between requests
                    var delay = _baseDelay + _random.Next(_requestDelay.Min, _requestDelay.Max);
                    if (delay > 0)
                        await Task.Delay(delay);
                }
            }

            // Show final stats for this user
            double userSuccessRate = userOrdersSubmitted > 0
                ? (double)userOrdersSucceeded / userOrdersSubmitted * 100
                : 0;

            await LogOrderActionAsync($"Completed random orders for user {user.Email}: {userOrdersSucceeded}/{userOrdersSubmitted} ({userSuccessRate:F1}% success rate)", verbose, consoleOutput);

            if (consoleOutput)
            {
                AnsiConsole.MarkupLine($"[blue]Completed random orders for {user.Email}:[/] {userOrdersSucceeded}/{userOrdersSubmitted} " +
                                    $"({userSuccessRate:F1}% success rate)");
            }
        }

        /// <summary>
        /// Submits market orders for a user
        /// </summary>
        /// <param name="user">The user credentials</param>
        /// <param name="numOrders">Number of orders to submit</param>
        /// <param name="ctx">Status context for progress reporting</param>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <param name="consoleOutput">Whether to output to console</param>
        public async Task SubmitMarketOrdersAsync(UserCredential user, int numOrders, StatusContext? ctx, bool verbose, bool consoleOutput = true)
        {
            int userOrdersSubmitted = 0;
            int userOrdersSucceeded = 0;

            await LogOrderActionAsync($"Starting market orders for user {user.Email}", verbose, consoleOutput);

            if (consoleOutput)
            {
                AnsiConsole.MarkupLine($"[blue]Starting market orders for {user.Email}[/]");
            }

            for (int i = 0; i < numOrders; i++)
            {
                var orderNumber = Interlocked.Increment(ref _totalOperations);
                userOrdersSubmitted++;

                try
                {
                    // Get the user-specific HTTP client for trading service
                    var httpClient = _httpClientFactory.GetUserClient(user.UserId, "trading");

                    // Check if authorization header is set correctly
                    var authHeader = httpClient.DefaultRequestHeaders.Authorization;
                    if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
                    {
                        // Re-apply the token if it's missing
                        _httpClientFactory.SetUserAuthToken(user.UserId, user.Token);
                        await LogOrderActionAsync($"Re-applying token for user {user.Email} before order submission", verbose, consoleOutput);
                    }

                    // Generate market order
                    var symbol = _symbolList[_random.Next(_symbolList.Count)];
                    var orderRequest = SimulationStrategies.GenerateMarketOrder(symbol);

                    // Log detailed order information
                    await LogOrderActionAsync($"Creating market order #{orderNumber} for user {user.Email} - Symbol: {orderRequest.Symbol}, Side: {orderRequest.Side}, Type: {orderRequest.Type}, Quantity: {orderRequest.Quantity}", verbose, consoleOutput);

                    // Log the order being submitted - fix markup issue by not trying to style the order type
                    if (consoleOutput)
                    {
                        AnsiConsole.MarkupLine($"[grey]Submitting market order {orderNumber}: {orderRequest.Side} {orderRequest.Quantity} {orderRequest.Symbol} (MARKET)[/]");
                    }

                    // Submit the order and measure latency
                    var stopwatch = Stopwatch.StartNew();
                    var response = await httpClient.PostAsJsonAsync("/order", orderRequest);
                    stopwatch.Stop();
                    _latencies.Add(stopwatch.ElapsedMilliseconds);

                    // Process the response
                    await ProcessResponseAsync(response, orderRequest, user, verbose, consoleOutput);

                    // Update counters based on success/failure
                    if (response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref _successCount);
                        userOrdersSucceeded++;

                        // Update progress if available
                        if (ctx != null && consoleOutput)
                        {
                            ctx.Status($"Processing market order {orderNumber}/{numOrders}");
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref _failureCount);
                    }
                }
                catch (Exception ex)
                {
                    await LogOrderActionAsync($"Error submitting market order: {ex.Message}", verbose, consoleOutput);

                    if (consoleOutput)
                    {
                        AnsiConsole.MarkupLine($"[red]Error submitting market order: {ex.Message}[/]");
                    }

                    Interlocked.Increment(ref _failureCount);
                }
                finally
                {
                    // Add some delay between requests
                    var delay = _baseDelay + _random.Next(_requestDelay.Min, _requestDelay.Max);
                    if (delay > 0)
                        await Task.Delay(delay);
                }

                // Show current stats occasionally (every 5 orders or at the end)
                if (consoleOutput && (i % 5 == 0 || i == numOrders - 1))
                {
                    double currentSuccessRate = _totalOperations > 0
                        ? (double)_successCount / _totalOperations * 100
                        : 0;

                    AnsiConsole.MarkupLine($"[grey]Progress: {orderNumber}/{_ordersPerUser * _totalUsers} total orders | " +
                                        $"Success rate: {currentSuccessRate:F1}% | " +
                                        $"Current user: {userOrdersSucceeded}/{userOrdersSubmitted}[/]");
                }
            }

            // Show final stats for this user
            double userSuccessRate = userOrdersSubmitted > 0
                ? (double)userOrdersSucceeded / userOrdersSubmitted * 100
                : 0;

            await LogOrderActionAsync($"Completed market orders for user {user.Email}: {userOrdersSucceeded}/{userOrdersSubmitted} ({userSuccessRate:F1}% success rate)", verbose, consoleOutput);

            if (consoleOutput)
            {
                AnsiConsole.MarkupLine($"[blue]Completed market orders for {user.Email}:[/] {userOrdersSucceeded}/{userOrdersSubmitted} " +
                                    $"({userSuccessRate:F1}% success rate)");
            }
        }

        /// <summary>
        /// Process HTTP response and log results
        /// </summary>
        /// <param name="response">The HTTP response</param>
        /// <param name="orderRequest">The original order request</param>
        /// <param name="user">The user who created the order</param>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <param name="consoleOutput">Whether to output to console</param>
        private async Task ProcessResponseAsync(
            HttpResponseMessage response,
            CreateOrderRequest orderRequest,
            UserCredential user,
            bool verbose,
            bool consoleOutput = true)
        {
            // Read response content
            string content = await response.Content.ReadAsStringAsync();

            // Log response to file for all requests
            await LogResponseAsync(
                $"=== Order Response ===\n" +
                $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"User: {user.Email} (UserId: {user.UserId})\n" +
                $"Order: Symbol: {orderRequest.Symbol}, Side: {orderRequest.Side}, Type: {orderRequest.Type}\n" +
                $"Status: {response.StatusCode}\n" +
                $"Response: {content}\n");

            // Additionally log it to console if verbose and it's not a success
            if (!response.IsSuccessStatusCode)
            {
                await LogOrderActionAsync($"Order failed for user {user.Email} - Status: {response.StatusCode}, Response: {content}", verbose, consoleOutput);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await LogOrderActionAsync($"Unauthorized error detected - Token: {user.Token.Substring(0, Math.Min(20, user.Token.Length))}...", verbose, consoleOutput);
                }

                // Don't increment the failure count here, it's already done in the calling methods
            }
            else
            {
                await LogOrderActionAsync($"Order succeeded for user {user.Email} - Status: {response.StatusCode}", verbose, consoleOutput);
                // Don't increment the success count here, it's already done in the calling methods
            }
        }

        /// <summary>
        /// Logs an API response to file
        /// </summary>
        /// <param name="message">Message to log</param>
        private async Task LogResponseAsync(string message)
        {
            lock (_logLock)
            {
                using var writer = new StreamWriter(_responseLogFile, true);
                writer.WriteLine(message);
                writer.WriteLine("-------------------\n");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Log order action to a detailed log file
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="verbose">Whether to also display in console</param>
        /// <param name="consoleOutput">Whether console output is enabled</param>
        private Task LogOrderActionAsync(string message, bool verbose, bool consoleOutput = true)
        {
            // Log to file (always)
            lock (_logLock)
            {
                using var writer = new StreamWriter(_orderLogFile, true);
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }

            // Optionally log to console if verbose AND console output is enabled
            if (verbose && consoleOutput)
            {
                AnsiConsole.MarkupLine($"[grey][{DateTime.Now:HH:mm:ss}] {message}[/]");
            }

            return Task.CompletedTask;
        }
    }
}