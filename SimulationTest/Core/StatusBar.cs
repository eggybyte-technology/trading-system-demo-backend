using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace SimulationTest.Core
{
    /// <summary>
    /// Dynamic status bar that displays test progress and statistics using Spectre.Console
    /// </summary>
    public class StatusBar
    {
        private int _successCount;
        private int _failureCount;
        private long _totalLatencyMs;
        private readonly Stopwatch _stopwatch;
        private readonly int _totalOperations;
        private int _completedOperations;
        private readonly object _lock = new();
        private bool _isRunning = false;
        private ProgressTask _progressTask;
        private ManualResetEvent _completionEvent = new ManualResetEvent(false);
        private readonly List<long> _latencies = new List<long>(); // To store individual latency values for percentile calculations

        // Separate tracking for order creation metrics
        private int _orderSuccessCount;
        private int _orderFailureCount;
        private long _orderTotalLatencyMs;
        private readonly List<long> _orderLatencies = new List<long>();

        /// <summary>
        /// Creates a new status bar
        /// </summary>
        /// <param name="totalOperations">Total number of operations to perform</param>
        public StatusBar(int totalOperations)
        {
            _successCount = 0;
            _failureCount = 0;
            _totalLatencyMs = 0;
            _stopwatch = new Stopwatch();
            _totalOperations = totalOperations;
            _completedOperations = 0;

            // Initialize order metrics
            _orderSuccessCount = 0;
            _orderFailureCount = 0;
            _orderTotalLatencyMs = 0;

            // Reserve a line for the progress display
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Start the status bar timer
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
            _isRunning = true;

            // Run the progress display in a separate thread to avoid blocking
            Task.Run(() =>
            {
                AnsiConsole.Progress()
                    .AutoClear(false)
                    .HideCompleted(false)
                    .Columns(new ProgressColumn[]
                    {
                        new SpinnerColumn(),
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn
                        {
                            CompletedStyle = new Style(foreground: Color.Green),
                            FinishedStyle = new Style(foreground: Color.Green),
                            IndeterminateStyle = new Style(foreground: Color.Yellow)
                        },
                        new PercentageColumn(),
                        new RemainingTimeColumn(),
                        new ElapsedTimeColumn()
                    })
                    .Start(ctx =>
                    {
                        _progressTask = ctx.AddTask("[green]Testing Progress[/]", maxValue: _totalOperations);

                        // Update the progress bar until test completes
                        while (_isRunning)
                        {
                            UpdateProgressTask();
                            Thread.Sleep(100);  // Update every 100ms
                        }

                        // Ensure 100% is displayed at the end
                        _progressTask.Value = _totalOperations;
                        double orderRps = GetOrderRequestsPerSecond();
                        double successRate = (_successCount + _failureCount) > 0
                            ? (double)_successCount / (_successCount + _failureCount) * 100
                            : 0;
                        _progressTask.Description = $"[green]Testing Complete[/] - Success: {_successCount} | Failures: {_failureCount} | Success Rate: {successRate:F2}% | Order RPS: {orderRps:F2}";

                        // Signal that the progress has been updated to completion
                        _completionEvent.Set();
                    });
            });
        }

        private void UpdateProgressTask()
        {
            lock (_lock)
            {
                if (_progressTask != null)
                {
                    _progressTask.Value = Math.Min(_completedOperations, _totalOperations);
                    double successRate = (_successCount + _failureCount) > 0
                        ? (double)_successCount / (_successCount + _failureCount) * 100
                        : 0;
                    double orderRps = GetOrderRequestsPerSecond();
                    _progressTask.Description = $"[green]Testing Progress[/] - Success: {_successCount} | Failures: {_failureCount} | Success Rate: {successRate:F2}% | Order RPS: {orderRps:F2}";
                }
            }
        }

        /// <summary>
        /// Report a successful operation
        /// </summary>
        /// <param name="latencyMs">Latency of the operation in milliseconds</param>
        /// <param name="isOrderCreation">Whether this is an order creation operation</param>
        public void ReportSuccess(long latencyMs, bool isOrderCreation = false)
        {
            lock (_lock)
            {
                _successCount++;
                _totalLatencyMs += latencyMs;
                _completedOperations++;
                _latencies.Add(latencyMs); // Store individual latency values

                // Track order metrics separately
                if (isOrderCreation)
                {
                    _orderSuccessCount++;
                    _orderTotalLatencyMs += latencyMs;
                    _orderLatencies.Add(latencyMs);
                }
            }
        }

        /// <summary>
        /// Report a failed operation
        /// </summary>
        /// <param name="isOrderCreation">Whether this is an order creation operation</param>
        public void ReportFailure(bool isOrderCreation = false)
        {
            lock (_lock)
            {
                _failureCount++;
                _completedOperations++;

                // Track order failures separately
                if (isOrderCreation)
                {
                    _orderFailureCount++;
                }
            }
        }

        private double GetAverageLatency() => _successCount > 0 ? (double)_totalLatencyMs / _successCount : 0;

        private double GetOrderAverageLatency() => _orderSuccessCount > 0 ? (double)_orderTotalLatencyMs / _orderSuccessCount : 0;

        private double GetRequestsPerSecond() => _stopwatch.ElapsedMilliseconds > 0
            ? _successCount / (_stopwatch.Elapsed.TotalSeconds)
            : 0;

        private double GetOrderRequestsPerSecond() => _stopwatch.ElapsedMilliseconds > 0
            ? _orderSuccessCount / (_stopwatch.Elapsed.TotalSeconds)
            : 0;

        /// <summary>
        /// Calculate percentile value from latency list
        /// </summary>
        private long CalculatePercentile(List<long> latencies, double percentile)
        {
            if (latencies.Count == 0)
                return 0;

            var sortedLatencies = new List<long>(latencies);
            sortedLatencies.Sort();

            int index = (int)Math.Ceiling(percentile / 100.0 * sortedLatencies.Count) - 1;
            index = Math.Max(0, Math.Min(sortedLatencies.Count - 1, index));
            return sortedLatencies[index];
        }

        /// <summary>
        /// Stop the status bar and return statistics
        /// </summary>
        /// <returns>Test statistics</returns>
        public TestStatistics Stop()
        {
            _stopwatch.Stop();
            _isRunning = false;

            // Wait for the progress bar to complete final update
            _completionEvent.WaitOne(1000);

            // Give a small delay for visual feedback
            Thread.Sleep(500);

            // Calculate detailed latency statistics for all operations
            long minLatency = 0;
            long maxLatency = 0;
            long p50Latency = 0;
            long p90Latency = 0;
            long p95Latency = 0;
            long p99Latency = 0;

            // Calculate detailed latency statistics for order operations
            long orderMinLatency = 0;
            long orderMaxLatency = 0;
            long orderP50Latency = 0;
            long orderP90Latency = 0;
            long orderP95Latency = 0;
            long orderP99Latency = 0;

            lock (_lock)
            {
                if (_latencies.Count > 0)
                {
                    minLatency = _latencies.Min();
                    maxLatency = _latencies.Max();
                    p50Latency = CalculatePercentile(_latencies, 50);
                    p90Latency = CalculatePercentile(_latencies, 90);
                    p95Latency = CalculatePercentile(_latencies, 95);
                    p99Latency = CalculatePercentile(_latencies, 99);
                }

                if (_orderLatencies.Count > 0)
                {
                    orderMinLatency = _orderLatencies.Min();
                    orderMaxLatency = _orderLatencies.Max();
                    orderP50Latency = CalculatePercentile(_orderLatencies, 50);
                    orderP90Latency = CalculatePercentile(_orderLatencies, 90);
                    orderP95Latency = CalculatePercentile(_orderLatencies, 95);
                    orderP99Latency = CalculatePercentile(_orderLatencies, 99);
                }
            }

            // Calculate correct success rate
            double successRate = (_successCount + _failureCount) > 0
                ? (double)_successCount / (_successCount + _failureCount) * 100
                : 0;

            // Simple completion message instead of panels
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Test completed with {_completedOperations} operations, {_successCount} successful, {_failureCount} failed.[/]");
            AnsiConsole.WriteLine();

            return new TestStatistics
            {
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                TotalOperations = _completedOperations,
                AverageLatencyMs = GetAverageLatency(),
                RequestsPerSecond = GetRequestsPerSecond(),
                ElapsedTime = _stopwatch.Elapsed,
                MinLatencyMs = minLatency,
                MaxLatencyMs = maxLatency,
                P50LatencyMs = p50Latency,
                P90LatencyMs = p90Latency,
                P95LatencyMs = p95Latency,
                P99LatencyMs = p99Latency,
                // Add order-specific stats
                OrderSuccessCount = _orderSuccessCount,
                OrderFailureCount = _orderFailureCount,
                OrderAverageLatencyMs = GetOrderAverageLatency(),
                OrderRequestsPerSecond = GetOrderRequestsPerSecond(),
                OrderMinLatencyMs = orderMinLatency,
                OrderMaxLatencyMs = orderMaxLatency,
                OrderP50LatencyMs = orderP50Latency,
                OrderP90LatencyMs = orderP90Latency,
                OrderP95LatencyMs = orderP95Latency,
                OrderP99LatencyMs = orderP99Latency
            };
        }
    }

    /// <summary>
    /// Statistics for a test run
    /// </summary>
    public class TestStatistics
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalOperations { get; set; }
        public double AverageLatencyMs { get; set; }
        public double RequestsPerSecond { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public long MinLatencyMs { get; set; }
        public long MaxLatencyMs { get; set; }
        public long P50LatencyMs { get; set; }
        public long P90LatencyMs { get; set; }
        public long P95LatencyMs { get; set; }
        public long P99LatencyMs { get; set; }
        public List<OperationResult> Results { get; set; } = new();

        // Order-specific metrics
        public int OrderSuccessCount { get; set; }
        public int OrderFailureCount { get; set; }
        public double OrderAverageLatencyMs { get; set; }
        public double OrderRequestsPerSecond { get; set; }
        public long OrderMinLatencyMs { get; set; }
        public long OrderMaxLatencyMs { get; set; }
        public long OrderP50LatencyMs { get; set; }
        public long OrderP90LatencyMs { get; set; }
        public long OrderP95LatencyMs { get; set; }
        public long OrderP99LatencyMs { get; set; }

        public double SuccessRate => (SuccessCount + FailureCount) > 0
            ? (double)SuccessCount / (SuccessCount + FailureCount) * 100
            : 0;

        public double OrderSuccessRate => (OrderSuccessCount + OrderFailureCount) > 0
            ? (double)OrderSuccessCount / (OrderSuccessCount + OrderFailureCount) * 100
            : 0;
    }

    /// <summary>
    /// Result of a single operation
    /// </summary>
    public class OperationResult
    {
        public string OperationType { get; set; }
        public string UserId { get; set; }
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsOrderCreation => OperationType != null && OperationType.Contains("Create") && !OperationType.Contains("User");
    }
}