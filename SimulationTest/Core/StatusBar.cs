using System.Diagnostics;
using System.Text;

namespace SimulationTest.Core
{
    /// <summary>
    /// Dynamic status bar that displays test progress and statistics
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
        private readonly int _statusBarWidth;
        private readonly int _originalTop;

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
            _statusBarWidth = Console.WindowWidth - 10;
            _originalTop = Console.CursorTop;

            // Reserve lines for the status bar
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }

        /// <summary>
        /// Start the status bar timer
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
            Update();
        }

        /// <summary>
        /// Report a successful operation
        /// </summary>
        /// <param name="latencyMs">Latency of the operation in milliseconds</param>
        public void ReportSuccess(long latencyMs)
        {
            lock (_lock)
            {
                _successCount++;
                _totalLatencyMs += latencyMs;
                _completedOperations++;
                Update();
            }
        }

        /// <summary>
        /// Report a failed operation
        /// </summary>
        public void ReportFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _completedOperations++;
                Update();
            }
        }

        /// <summary>
        /// Update the status bar display
        /// </summary>
        private void Update()
        {
            int originalTop = Console.CursorTop;
            int originalLeft = Console.CursorLeft;

            // Update metrics
            double averageLatency = _successCount > 0 ? (double)_totalLatencyMs / _successCount : 0;
            double requestsPerSecond = _stopwatch.ElapsedMilliseconds > 0
                ? _successCount / (_stopwatch.Elapsed.TotalSeconds)
                : 0;
            double percentComplete = (double)_completedOperations / _totalOperations * 100;
            int progressBarFill = (int)Math.Round(percentComplete * _statusBarWidth / 100);

            // Build status bar
            var statsLine = $"Success: {_successCount} | Failures: {_failureCount} | Avg Latency: {averageLatency:F2}ms | RPS: {requestsPerSecond:F2}";
            var progressBar = new StringBuilder();
            progressBar.Append('[');
            progressBar.Append('=', progressBarFill);
            if (progressBarFill < _statusBarWidth)
                progressBar.Append('>');
            progressBar.Append(' ', _statusBarWidth - progressBarFill - (progressBarFill < _statusBarWidth ? 1 : 0));
            progressBar.Append(']');
            progressBar.Append($" {percentComplete:F2}% - {TimeSpan.FromMilliseconds(_stopwatch.ElapsedMilliseconds):hh\\:mm\\:ss}");

            // Save cursor and move to status bar position
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, _originalTop);

            // Clear and redraw status lines
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, _originalTop);
            Console.WriteLine(statsLine);

            Console.SetCursorPosition(0, _originalTop + 1);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, _originalTop + 1);
            Console.WriteLine(progressBar.ToString());

            // Restore cursor
            Console.SetCursorPosition(originalLeft, originalTop);
            Console.CursorVisible = true;
        }

        /// <summary>
        /// Stop the status bar and return statistics
        /// </summary>
        /// <returns>Test statistics</returns>
        public TestStatistics Stop()
        {
            _stopwatch.Stop();

            // Add blank line after status bar
            Console.SetCursorPosition(0, _originalTop + 3);
            Console.WriteLine();

            // Return final statistics
            return new TestStatistics
            {
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                TotalOperations = _totalOperations,
                AverageLatencyMs = _successCount > 0 ? (double)_totalLatencyMs / _successCount : 0,
                RequestsPerSecond = _stopwatch.ElapsedMilliseconds > 0
                    ? _successCount / (_stopwatch.Elapsed.TotalSeconds)
                    : 0,
                ElapsedTime = _stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Contains statistics for a completed test
    /// </summary>
    public class TestStatistics
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalOperations { get; set; }
        public double AverageLatencyMs { get; set; }
        public double RequestsPerSecond { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public List<OperationResult> Results { get; set; } = new();

        public double SuccessRate => TotalOperations > 0
            ? (double)SuccessCount / TotalOperations * 100
            : 0;
    }

    /// <summary>
    /// Represents the result of a single operation
    /// </summary>
    public class OperationResult
    {
        public string OperationType { get; set; }
        public string UserId { get; set; }
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
    }
}