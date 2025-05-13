using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SimulationTest.Helpers;
using Spectre.Console;
using CommonLib.Models.Identity;
using System.IO;

namespace SimulationTest.Core
{
    /// <summary>
    /// Runs stress tests with a simplified workflow: registering users and submitting orders
    /// </summary>
    public class StressTestRunner
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClientFactory _httpClientFactory;
        private readonly ConcurrentBag<RequestResult> _results = new ConcurrentBag<RequestResult>();
        private readonly object _updateLock = new object();
        private readonly string _simulationMode;
        private readonly List<string> _symbolList = new List<string> { "BTC-USDT", "ETH-USDT", "BNB-USDT" };
        private readonly Dictionary<string, (decimal Min, decimal Max)> _priceRanges = new() {
            { "BTC-USDT", (1000m, 100000m) },
            { "ETH-USDT", (100m, 10000m) },
            { "BNB-USDT", (10m, 1000m) }
        };

        // Test configuration
        private readonly int _userCount;
        private readonly int _ordersPerUser;
        private readonly int _concurrency;
        private readonly int _timeoutSeconds;

        // Test results
        private int _totalRequests;
        private int _successfulRequests;
        private int _failedRequests;
        private DateTime _startTime;
        private DateTime _endTime;

        // Progress tracking
        private readonly IProgress<TestProgress> _progress;
        private readonly string _testFolderPath;

        // Additional tracking for real-time metrics
        private readonly ConcurrentBag<double> _recentLatencies = new ConcurrentBag<double>();
        private int _recentSuccessfulRequests = 0;
        private int _recentFailedRequests = 0;
        private readonly object _metricsLock = new object();

        /// <summary>
        /// Initializes a new instance of the StressTestRunner class
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="userCount">Number of users to register</param>
        /// <param name="ordersPerUser">Number of orders per user</param>
        /// <param name="concurrency">Number of concurrent operations</param>
        /// <param name="timeoutSeconds">Timeout in seconds for HTTP requests</param>
        /// <param name="progress">Progress reporter</param>
        public StressTestRunner(
            IConfiguration configuration,
            int userCount,
            int ordersPerUser,
            int concurrency,
            int timeoutSeconds,
            IProgress<TestProgress> progress = null)
        {
            _configuration = configuration;
            _userCount = userCount;
            _ordersPerUser = ordersPerUser;
            _concurrency = concurrency;
            _timeoutSeconds = timeoutSeconds;
            _progress = progress;
            _simulationMode = configuration["StressTestSettings:SimulationMode"] ?? "random";
            _testFolderPath = configuration["StressTestSettings:TestFolderPath"];

            // Initialize HTTP client factory
            _httpClientFactory = new HttpClientFactory();
            _httpClientFactory.Configure(_timeoutSeconds);

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
        }

        /// <summary>
        /// Runs the stress test with the configured parameters
        /// </summary>
        /// <returns>A TestResult object containing test statistics</returns>
        public async Task<TestResult> RunAsync()
        {
            _startTime = DateTime.Now;

            // Verify connectivity to required services
            var servicesToCheck = new Dictionary<string, string>
            {
                { "trading", _httpClientFactory.GetServiceUrl("trading") },
                { "identity", _httpClientFactory.GetServiceUrl("identity") }
            };

            // Log test start
            UpdateProgress("Starting connectivity check", 0);
            Console.WriteLine($"Starting stress test at {_startTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Configuration: {_userCount} users, {_ordersPerUser} orders per user, {_concurrency} concurrent operations");

            var connectivityChecker = new ServiceConnectivityChecker(_httpClientFactory, servicesToCheck);
            var serviceStatusMap = await connectivityChecker.CheckAllServicesAsync();
            bool areServicesAvailable = serviceStatusMap.Values.All(status => status);

            if (!areServicesAvailable)
            {
                // Display which services are not available
                foreach (var service in serviceStatusMap.Where(s => !s.Value))
                {
                    Console.WriteLine($"Error: Service '{service.Key}' is not available");
                }
                Console.WriteLine("Cannot run trading simulation when required services are not available");

                return new TestResult
                {
                    Success = false,
                    ErrorMessage = "One or more required services are not available",
                    TotalRequests = 0,
                    SuccessfulRequests = 0,
                    FailedRequests = 0,
                    StartTime = _startTime,
                    EndTime = DateTime.Now,
                    ElapsedTime = DateTime.Now - _startTime
                };
            }

            // Reset counters
            _totalRequests = 0;
            _successfulRequests = 0;
            _failedRequests = 0;
            _results.Clear();

            try
            {
                _startTime = DateTime.Now;
                var stopwatch = Stopwatch.StartNew();

                // Create a timestamp-based folder for this test run
                string testFolderPath = _testFolderPath;
                if (string.IsNullOrEmpty(testFolderPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    testFolderPath = Path.Combine("logs", $"stress_test_{timestamp}");
                    Directory.CreateDirectory(testFolderPath);
                }

                // Initialize services (10% of progress)
                UpdateProgress("Initializing test services...", 0);

                // Create user service and market data service with the test folder path
                var userService = new UserService(
                    _httpClientFactory.GetClient("identity"),
                    _httpClientFactory,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase },
                    testFolderPath);

                UpdateProgress("Services initialized", 10);

                // Register users (20% of progress)
                UpdateProgress("Registering test users...", 10, 0, _userCount);

                var users = await userService.CreateTestUsersAsync(_userCount, verbose: false, _progress);

                if (users.Count == 0)
                {
                    Console.WriteLine("Failed to create test users. Aborting simulation.");
                    return new TestResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create test users",
                        TotalRequests = 0,
                        SuccessfulRequests = 0,
                        FailedRequests = 0,
                        StartTime = _startTime,
                        EndTime = DateTime.Now,
                        ElapsedTime = DateTime.Now - _startTime
                    };
                }

                UpdateProgress("All users registered", 30, users.Count, _userCount);

                // Submit orders (60% of progress)
                // Create order service with all required parameters
                var orderService = new OrderService(
                    _httpClientFactory,
                    users.Count,
                    _ordersPerUser,
                    _symbolList,
                    _priceRanges,
                    0, // No base delay
                    (0, 50) // Request delay range
                );

                // Calculate total operations and start progress display
                int totalOperations = users.Count * _ordersPerUser;
                UpdateProgress("Submitting orders...", 30, 0, totalOperations);

                // Reset counters before processing orders
                _successfulRequests = 0;
                _failedRequests = 0;
                _recentSuccessfulRequests = 0;
                _recentFailedRequests = 0;
                _recentLatencies.Clear();

                // Process users in batches to control concurrency
                var batchSize = Math.Min(_concurrency, users.Count);

                // Process all users with controlled concurrency
                for (int i = 0; i < users.Count; i += batchSize)
                {
                    var currentBatch = users.Skip(i).Take(batchSize).ToList();
                    var tasks = new List<Task>();

                    foreach (var user in currentBatch)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                // Create an event handler for order processing
                                var orderProcessed = new EventHandler<(bool success, double latencyMs)>((sender, data) =>
                                {
                                    // Record metrics for real-time reporting
                                    if (data.success)
                                    {
                                        RecordSuccess(data.latencyMs);
                                    }
                                    else
                                    {
                                        RecordFailure();
                                    }
                                });

                                // Pass the event handler to the order service
                                if (_simulationMode.Equals("market", StringComparison.OrdinalIgnoreCase))
                                {
                                    await orderService.SubmitMarketOrdersAsync(user, _ordersPerUser, null, false, false, orderProcessed);
                                }
                                else // Default to random mode
                                {
                                    await orderService.SubmitRandomOrdersAsync(user, _ordersPerUser, null, false, false, orderProcessed);
                                }

                                // No need to update progress here as each individual order completion
                                // will trigger a progress update via the RecordSuccess/RecordFailure methods
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing orders for user {user.Username}: {ex.Message}");
                            }
                        }));
                    }

                    // Wait for current batch to complete
                    await Task.WhenAll(tasks);
                }

                stopwatch.Stop();

                // Total requests is the sum of successful and failed requests
                _totalRequests = _successfulRequests + _failedRequests;

                // The individual latencies and results have already been recorded by RecordSuccess/RecordFailure

                _endTime = DateTime.Now;
                var elapsedTime = _endTime - _startTime;

                UpdateProgress("Test completed", 100);
                Console.WriteLine($"Test completed in {elapsedTime.TotalSeconds:F2} seconds");
                Console.WriteLine($"Total requests: {_totalRequests}, Successful: {_successfulRequests}, Failed: {_failedRequests}");

                // Calculate statistics
                TimeSpan averageLatency = TimeSpan.Zero;
                TimeSpan minLatency = TimeSpan.MaxValue;
                TimeSpan maxLatency = TimeSpan.Zero;

                List<TimeSpan> latencies = _results.Select(r => r.Duration).ToList();

                if (latencies.Count > 0)
                {
                    averageLatency = TimeSpan.FromTicks((long)latencies.Average(l => l.Ticks));
                    minLatency = TimeSpan.FromTicks(latencies.Min(l => l.Ticks));
                    maxLatency = TimeSpan.FromTicks(latencies.Max(l => l.Ticks));
                }

                // Calculate percentiles
                var percentiles = CalculatePercentiles(latencies);

                // Calculate orders per second
                var ordersPerSecond = elapsedTime.TotalSeconds > 0
                    ? totalOperations / elapsedTime.TotalSeconds
                    : 0;

                // Final progress report
                ReportProgress(
                    "Test completed",
                    100,
                    totalOperations,
                    totalOperations,
                    $"Test completed - Total: {totalOperations}, Success: {_successfulRequests}, Failed: {_failedRequests}, Time: {elapsedTime.TotalSeconds:F2}s"
                );

                // Send final progress update with IsFinal flag
                if (_progress != null)
                {
                    _progress.Report(new TestProgress
                    {
                        Message = "Test completed",
                        Percentage = 100,
                        Completed = totalOperations,
                        Total = totalOperations,
                        Passed = _successfulRequests,
                        Failed = _failedRequests,
                        AverageLatency = averageLatency.TotalMilliseconds,
                        SuccessRate = _successfulRequests > 0 ? (double)_successfulRequests / totalOperations * 100 : 0,
                        OperationsPerSecond = ordersPerSecond,
                        LogMessage = $"Test completed - Success: {_successfulRequests}, Failed: {_failedRequests}, Total: {totalOperations}, Time: {elapsedTime.TotalSeconds:F2}s",
                        IsFinal = true
                    });
                }

                return new TestResult
                {
                    Success = true,
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    AverageLatency = averageLatency,
                    MinLatency = minLatency,
                    MaxLatency = maxLatency,
                    Percentiles = percentiles,
                    StartTime = _startTime,
                    EndTime = _endTime,
                    ElapsedTime = elapsedTime,
                    OrdersPerSecond = ordersPerSecond,
                    TestFolderPath = testFolderPath
                };
            }
            catch (Exception ex)
            {
                _endTime = DateTime.Now;
                Console.WriteLine($"Error running stress test: {ex.Message}");

                // Make sure we have a test folder path even in case of error
                string testFolderPath = _testFolderPath;
                if (string.IsNullOrEmpty(testFolderPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    testFolderPath = Path.Combine("logs", $"stress_test_error_{timestamp}");
                    try { Directory.CreateDirectory(testFolderPath); } catch { }
                }

                return new TestResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    StartTime = _startTime,
                    EndTime = _endTime,
                    ElapsedTime = _endTime - _startTime,
                    TestFolderPath = testFolderPath
                };
            }
        }

        /// <summary>
        /// Calculates percentiles (50th, 90th, 95th, 99th) from a list of latencies
        /// </summary>
        private List<double> CalculatePercentiles(List<TimeSpan> latencies)
        {
            if (latencies == null || latencies.Count == 0)
                return new List<double> { 0, 0, 0, 0 };

            var sortedLatencies = latencies.OrderBy(l => l.TotalMilliseconds).ToList();
            int count = sortedLatencies.Count;

            double p50 = count > 0 ? sortedLatencies[(int)(count * 0.5)].TotalMilliseconds : 0;
            double p90 = count > 0 ? sortedLatencies[(int)(count * 0.9)].TotalMilliseconds : 0;
            double p95 = count > 0 ? sortedLatencies[(int)(count * 0.95)].TotalMilliseconds : 0;
            double p99 = count > 0 ? sortedLatencies[(int)(count * 0.99)].TotalMilliseconds : 0;

            return new List<double> { p50, p90, p95, p99 };
        }

        /// <summary>
        /// Updates the progress tracker
        /// </summary>
        private void UpdateProgress(string message, int percentage, int completed = 0, int total = 0)
        {
            // Replace any square brackets in the message to avoid markup parsing errors
            string safeMessage = message?.Replace("[", "").Replace("]", "") ?? "";

            ReportProgress(safeMessage, percentage, completed, total, null);
        }

        /// <summary>
        /// Reports detailed progress information
        /// </summary>
        private void ReportProgress(string message, int percentage, int completed, int total, string logMessage)
        {
            if (_progress != null)
            {
                var elapsedTime = DateTime.Now - _startTime;
                var completedOps = GetCompletedOperations();
                var successRate = GetCurrentSuccessRate();
                var avgLatency = GetCurrentAverageLatency();
                var opsPerSecond = elapsedTime.TotalSeconds > 0 ? completedOps / elapsedTime.TotalSeconds : 0;

                _progress.Report(new TestProgress
                {
                    Message = message,
                    Percentage = percentage,
                    Completed = completed,
                    Total = total,
                    AverageLatency = avgLatency,
                    SuccessRate = successRate,
                    OperationsPerSecond = opsPerSecond,
                    LogMessage = logMessage ?? $"Processing: {completed}/{total}"
                });
            }
        }

        /// <summary>
        /// Gets the current success rate based on completed operations
        /// </summary>
        /// <returns>The success rate as a percentage</returns>
        public double GetCurrentSuccessRate()
        {
            int successCount = _recentSuccessfulRequests;
            int totalCount = successCount + _recentFailedRequests;

            if (totalCount == 0)
                return 100.0; // No requests yet

            return (double)successCount / totalCount * 100.0;
        }

        /// <summary>
        /// Gets the current average latency based on completed operations
        /// </summary>
        /// <returns>The average latency in milliseconds</returns>
        public double GetCurrentAverageLatency()
        {
            if (_recentLatencies.IsEmpty)
                return 0.0;

            return _recentLatencies.Average();
        }

        /// <summary>
        /// Records a successful operation with its latency
        /// </summary>
        /// <param name="latencyMs">The latency in milliseconds</param>
        private void RecordSuccess(double latencyMs)
        {
            // Update both the total and recent counters
            Interlocked.Increment(ref _successfulRequests);
            Interlocked.Increment(ref _recentSuccessfulRequests);
            _results.Add(new RequestResult { Success = true, Duration = TimeSpan.FromMilliseconds(latencyMs), StatusCode = 200 });
            _recentLatencies.Add(latencyMs);

            // Keep the latency collection from growing too large
            if (_recentLatencies.Count > 1000)
            {
                // Just allow it to reset for simplicity
                var newLatencies = new ConcurrentBag<double>();
                _recentLatencies.Take(100).ToList().ForEach(l => newLatencies.Add(l));
                // We don't need to worry about thread safety here as this is just for UI display
            }

            // Update progress to reflect this completed operation
            UpdateOrderProgress();
        }

        /// <summary>
        /// Records a failed operation
        /// </summary>
        private void RecordFailure()
        {
            // Update both the total and recent counters
            Interlocked.Increment(ref _failedRequests);
            Interlocked.Increment(ref _recentFailedRequests);

            // Update progress to reflect this completed operation
            UpdateOrderProgress();
        }

        /// <summary>
        /// Updates the progress display with the latest order completion information
        /// </summary>
        private void UpdateOrderProgress()
        {
            // Calculate total completed operations
            int completed = _successfulRequests + _failedRequests;
            int total = _userCount * _ordersPerUser;

            // Calculate percentage (from 30% to 90%)
            int progressPercent = 30 + (int)((float)completed / total * 60);

            // Prepare detailed log message
            string logMessage = $"Submitting orders: {completed}/{total} completed - " +
                $"Success rate: {GetCurrentSuccessRate():F2}% - " +
                $"Avg Latency: {GetCurrentAverageLatency():F2}ms";

            // Update progress with detailed stats
            ReportProgress("Submitting orders...", progressPercent, completed, total, logMessage);
        }

        /// <summary>
        /// Gets the number of operations completed so far
        /// </summary>
        /// <returns>The count of completed operations</returns>
        public int GetCompletedOperations()
        {
            return _successfulRequests + _failedRequests;
        }
    }

    /// <summary>
    /// Represents a single request result
    /// </summary>
    public class RequestResult
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public int StatusCode { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Represents the result of a stress test
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public TimeSpan AverageLatency { get; set; }
        public TimeSpan MinLatency { get; set; }
        public TimeSpan MaxLatency { get; set; }
        public List<double> Percentiles { get; set; } = new List<double>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public double OrdersPerSecond { get; set; }
        public string TestFolderPath { get; set; }
    }
}