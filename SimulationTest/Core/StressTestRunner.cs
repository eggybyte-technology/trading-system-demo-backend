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

namespace SimulationTest.Core
{
    /// <summary>
    /// Runs stress tests against a specific service with real-time progress tracking
    /// </summary>
    public class StressTestRunner
    {
        private readonly IConfiguration _configuration;
        private readonly string _targetService;
        private readonly int _concurrency;
        private readonly int _durationSeconds;
        private readonly HttpClientFactory _httpClientFactory;
        private readonly ConcurrentBag<RequestResult> _results = new ConcurrentBag<RequestResult>();
        private CancellationTokenSource _cts;
        private TestProgressTracker _progressTracker;
        private int _totalRequests;
        private int _successfulRequests;
        private int _failedRequests;
        private readonly object _updateLock = new object();

        /// <summary>
        /// Initializes a new instance of the StressTestRunner class
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        /// <param name="targetService">The service to target</param>
        /// <param name="concurrency">Number of concurrent requests</param>
        /// <param name="durationSeconds">Duration of the test in seconds</param>
        public StressTestRunner(IConfiguration configuration, string targetService, int concurrency, int durationSeconds)
        {
            _configuration = configuration;
            _targetService = targetService;
            _concurrency = concurrency;
            _durationSeconds = durationSeconds;

            // Initialize HTTP client factory
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

            // Initialize progress tracker (est. 10 requests per second per thread)
            int estimatedTotalRequests = _concurrency * _durationSeconds * 10;
            _progressTracker = new TestProgressTracker(estimatedTotalRequests);
        }

        /// <summary>
        /// Runs the stress test
        /// </summary>
        public async Task RunAsync()
        {
            // Verify connectivity to the target service
            var connectivityChecker = new ServiceConnectivityChecker(_httpClientFactory,
                new Dictionary<string, string> { { _targetService, _httpClientFactory.GetServiceUrl(_targetService) } });

            bool isServiceAvailable = await connectivityChecker.CheckServiceConnectivityAsync(_targetService);
            if (!isServiceAvailable)
            {
                AnsiConsole.MarkupLine($"[red]Error: {_targetService} service is not available[/]");
                return;
            }

            // Reset counters
            _totalRequests = 0;
            _successfulRequests = 0;
            _failedRequests = 0;
            _results.Clear();

            // Start progress tracking
            _progressTracker.StartTracking();

            // Create cancellation token source
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(_durationSeconds));

            try
            {
                AnsiConsole.MarkupLine($"[blue]Starting stress test against {_targetService} service with {_concurrency} concurrent users for {_durationSeconds} seconds[/]");

                // Create and start worker tasks
                var tasks = new List<Task>();
                for (int i = 0; i < _concurrency; i++)
                {
                    int workerIndex = i;
                    tasks.Add(Task.Run(() => RunWorkerAsync(workerIndex, _cts.Token)));
                }

                // Wait for all tasks to complete or for the time to expire
                await Task.WhenAll(tasks);

                // Summarize results
                _progressTracker.StopTracking();
                await SummarizeResultsAsync();
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Runs a worker task that sends requests to the target service
        /// </summary>
        /// <param name="workerIndex">The worker index</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task RunWorkerAsync(int workerIndex, CancellationToken cancellationToken)
        {
            // Get client for the target service
            var client = _httpClientFactory.GetClient(_targetService);

            // Select endpoint based on target service
            string endpoint = GetRandomEndpoint(_targetService);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    try
                    {
                        // Send request
                        var response = await client.GetAsync(endpoint, cancellationToken);

                        stopwatch.Stop();

                        // Record result
                        var result = new RequestResult
                        {
                            Success = response.IsSuccessStatusCode,
                            Duration = stopwatch.Elapsed,
                            StatusCode = (int)response.StatusCode
                        };

                        _results.Add(result);

                        lock (_updateLock)
                        {
                            _totalRequests++;
                            if (result.Success)
                            {
                                _successfulRequests++;
                            }
                            else
                            {
                                _failedRequests++;
                            }
                        }

                        // Update progress tracker
                        _progressTracker.UpdateTestResult(new ApiTestResult
                        {
                            Success = result.Success,
                            Duration = result.Duration,
                            Message = result.Success ? "Request succeeded" : $"Request failed with status code {result.StatusCode}"
                        });

                        // Add a small delay between requests (1-50ms)
                        await Task.Delay(new Random().Next(1, 50), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Test duration expired
                        break;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();

                        // Record failed result
                        var result = new RequestResult
                        {
                            Success = false,
                            Duration = stopwatch.Elapsed,
                            StatusCode = 0,
                            Exception = ex
                        };

                        _results.Add(result);

                        lock (_updateLock)
                        {
                            _totalRequests++;
                            _failedRequests++;
                        }

                        // Update progress tracker
                        _progressTracker.UpdateTestResult(new ApiTestResult
                        {
                            Success = false,
                            Duration = result.Duration,
                            Message = $"Request failed: {ex.Message}",
                            Exception = ex
                        });

                        // Add a delay between failed requests (100-200ms)
                        await Task.Delay(new Random().Next(100, 200), cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Test duration expired
            }
        }

        /// <summary>
        /// Gets a random endpoint for the target service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <returns>A random endpoint URL</returns>
        private string GetRandomEndpoint(string serviceName)
        {
            var random = new Random();

            switch (serviceName)
            {
                case "identity":
                    var identityEndpoints = new[]
                    {
                        "/health",
                        "/auth/user"
                    };
                    return identityEndpoints[random.Next(identityEndpoints.Length)];

                case "trading":
                    var tradingEndpoints = new[]
                    {
                        "/health",
                        "/order/open"
                    };
                    return tradingEndpoints[random.Next(tradingEndpoints.Length)];

                case "market-data":
                    var marketDataEndpoints = new[]
                    {
                        "/health",
                        "/market/symbols",
                        "/market/ticker?symbol=BTC-USDT",
                        "/market/summary"
                    };
                    return marketDataEndpoints[random.Next(marketDataEndpoints.Length)];

                case "account":
                    var accountEndpoints = new[]
                    {
                        "/health",
                        "/account/assets"
                    };
                    return accountEndpoints[random.Next(accountEndpoints.Length)];

                case "risk":
                    var riskEndpoints = new[]
                    {
                        "/health",
                        "/risk/limits"
                    };
                    return riskEndpoints[random.Next(riskEndpoints.Length)];

                case "notification":
                    var notificationEndpoints = new[]
                    {
                        "/health",
                        "/notifications"
                    };
                    return notificationEndpoints[random.Next(notificationEndpoints.Length)];

                case "match-making":
                    var matchMakingEndpoints = new[]
                    {
                        "/health",
                        "/match/status"
                    };
                    return matchMakingEndpoints[random.Next(matchMakingEndpoints.Length)];

                default:
                    return "/health";
            }
        }

        /// <summary>
        /// Summarizes the stress test results
        /// </summary>
        private async Task SummarizeResultsAsync()
        {
            // Calculate statistics
            int totalRequests = _results.Count;
            int successfulRequests = _results.Count(r => r.Success);
            int failedRequests = totalRequests - successfulRequests;
            double successRate = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 0;

            TimeSpan averageLatency = TimeSpan.Zero;
            TimeSpan minLatency = TimeSpan.MaxValue;
            TimeSpan maxLatency = TimeSpan.Zero;

            if (totalRequests > 0)
            {
                averageLatency = TimeSpan.FromTicks((long)_results.Average(r => r.Duration.Ticks));
                minLatency = TimeSpan.FromTicks(_results.Min(r => r.Duration.Ticks));
                maxLatency = TimeSpan.FromTicks(_results.Max(r => r.Duration.Ticks));
            }

            // Calculate requests per second
            double requestsPerSecond = totalRequests / (double)_durationSeconds;

            // Display summary
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Stress Test Results[/]");
            AnsiConsole.WriteLine($"Target Service: {_targetService}");
            AnsiConsole.WriteLine($"Duration: {_durationSeconds} seconds");
            AnsiConsole.WriteLine($"Concurrency: {_concurrency}");
            AnsiConsole.WriteLine($"Total Requests: {totalRequests}");
            AnsiConsole.WriteLine($"Successful Requests: {successfulRequests} ({successRate:F1}%)");
            AnsiConsole.WriteLine($"Failed Requests: {failedRequests}");
            AnsiConsole.WriteLine($"Requests Per Second: {requestsPerSecond:F1}");
            AnsiConsole.WriteLine($"Average Latency: {averageLatency.TotalMilliseconds:F2}ms");
            AnsiConsole.WriteLine($"Min Latency: {minLatency.TotalMilliseconds:F2}ms");
            AnsiConsole.WriteLine($"Max Latency: {maxLatency.TotalMilliseconds:F2}ms");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Represents the result of a stress test request
    /// </summary>
    internal class RequestResult
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public int StatusCode { get; set; }
        public Exception Exception { get; set; }
    }
}