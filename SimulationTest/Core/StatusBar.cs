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
                        _progressTask.Description = $"[green]Testing Complete[/] - Success: {_successCount} | Failures: {_failureCount} | Avg Latency: {GetAverageLatency():F2}ms | RPS: {GetRequestsPerSecond():F2}";

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
                    _progressTask.Description = $"[green]Testing Progress[/] - Success: {_successCount} | Failures: {_failureCount} | Avg Latency: {GetAverageLatency():F2}ms | RPS: {GetRequestsPerSecond():F2}";
                }
            }
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
            }
        }

        private double GetAverageLatency() => _successCount > 0 ? (double)_totalLatencyMs / _successCount : 0;

        private double GetRequestsPerSecond() => _stopwatch.ElapsedMilliseconds > 0
            ? _successCount / (_stopwatch.Elapsed.TotalSeconds)
            : 0;

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

            // Display final statistics in a table
            var table = new Table();
            table.AddColumn("[yellow]Statistic[/]");
            table.AddColumn("[yellow]Value[/]");

            table.AddRow("[green]Success Count[/]", _successCount.ToString());
            table.AddRow("[red]Failure Count[/]", _failureCount.ToString());
            table.AddRow("[blue]Total Operations[/]", _totalOperations.ToString());
            table.AddRow("[blue]Average Latency[/]", $"{GetAverageLatency():F2}ms");
            table.AddRow("[blue]Requests Per Second[/]", $"{GetRequestsPerSecond():F2}");
            table.AddRow("[blue]Elapsed Time[/]", $"{_stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");
            table.AddRow("[blue]Success Rate[/]", $"{(_totalOperations > 0 ? (double)_successCount / _totalOperations * 100 : 0):F2}%");

            // Add border and style to the table
            table.Border(TableBorder.Rounded);
            table.Expand();

            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Return final statistics
            return new TestStatistics
            {
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                TotalOperations = _totalOperations,
                AverageLatencyMs = GetAverageLatency(),
                RequestsPerSecond = GetRequestsPerSecond(),
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