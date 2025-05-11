using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spectre.Console;

namespace SimulationTest.Core
{
    /// <summary>
    /// Tracks and displays test progress in real-time
    /// </summary>
    public class TestProgressTracker
    {
        private int _totalTests;
        private int _completedTests;
        private int _succeededTests;
        private ConcurrentBag<TimeSpan> _latencies = new ConcurrentBag<TimeSpan>();
        private readonly object _lock = new object();
        private bool _isDisplaying;
        private Timer _refreshTimer;
        private readonly int _refreshIntervalMs;

        /// <summary>
        /// Initializes a new instance of the TestProgressTracker class
        /// </summary>
        /// <param name="totalTests">Total number of tests to run</param>
        /// <param name="refreshIntervalMs">Refresh interval in milliseconds</param>
        public TestProgressTracker(int totalTests, int refreshIntervalMs = 500)
        {
            _totalTests = totalTests;
            _completedTests = 0;
            _succeededTests = 0;
            _refreshIntervalMs = refreshIntervalMs;
        }

        /// <summary>
        /// Starts displaying progress
        /// </summary>
        public void StartTracking()
        {
            _isDisplaying = true;
            _refreshTimer = new Timer(UpdateDisplay, null, 0, _refreshIntervalMs);
        }

        /// <summary>
        /// Stops displaying progress
        /// </summary>
        public void StopTracking()
        {
            _isDisplaying = false;
            _refreshTimer?.Dispose();
        }

        /// <summary>
        /// Updates the test statistics with a completed test result
        /// </summary>
        /// <param name="result">The test result</param>
        public void UpdateTestResult(ApiTestResult result)
        {
            lock (_lock)
            {
                _completedTests++;

                if (result.Success)
                {
                    _succeededTests++;
                }

                _latencies.Add(result.Duration);
            }
        }

        /// <summary>
        /// Updates the display with current progress
        /// </summary>
        private void UpdateDisplay(object state)
        {
            if (!_isDisplaying) return;

            // Calculate progress values
            double progressPercentage = (_totalTests > 0) ? (double)_completedTests / _totalTests * 100 : 0;
            double successPercentage = (_completedTests > 0) ? (double)_succeededTests / _completedTests * 100 : 0;

            // Calculate average latency
            double avgLatencyMs = 0;
            if (_latencies.Count > 0)
            {
                avgLatencyMs = _latencies.Average(l => l.TotalMilliseconds);
            }

            // Calculate latency percentage (100ms = 100%)
            double latencyPercentage = Math.Min(avgLatencyMs / 100.0 * 100, 100);

            // Clear the current line and write progress
            int currentTop = Console.CursorTop;

            try
            {
                // Save the current cursor position
                Console.CursorVisible = false;

                // Move to the bottom of the console and print progress
                int maxConsoleRow = Math.Max(0, Console.WindowHeight - 4);
                Console.SetCursorPosition(0, maxConsoleRow);

                // Clear lines for progress display
                for (int i = 0; i < 3; i++)
                {
                    Console.SetCursorPosition(0, maxConsoleRow + i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }

                // Draw progress bars using Spectre.Console markup
                Console.SetCursorPosition(0, maxConsoleRow);
                AnsiConsole.MarkupLine($"Progress: [blue]{_completedTests}/{_totalTests}[/] ([blue]{progressPercentage:F1}%[/])");
                DrawProgressBar(progressPercentage);

                Console.SetCursorPosition(0, maxConsoleRow + 1);
                AnsiConsole.MarkupLine($"Success: [green]{_succeededTests}/{_completedTests}[/] ([green]{successPercentage:F1}%[/])");
                DrawProgressBar(successPercentage);

                Console.SetCursorPosition(0, maxConsoleRow + 2);
                string latencyColor = avgLatencyMs < 50 ? "green" : (avgLatencyMs < 100 ? "yellow" : "red");
                AnsiConsole.MarkupLine($"Avg Latency: [{latencyColor}]{avgLatencyMs:F0}ms[/]");
                DrawProgressBar(latencyPercentage, latencyColor);
            }
            catch (Exception)
            {
                // Ignore any console rendering errors
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        /// <summary>
        /// Draws a simple progress bar
        /// </summary>
        private void DrawProgressBar(double percentage, string color = "blue")
        {
            int width = Math.Min(50, Console.WindowWidth - 5);
            int filledWidth = (int)(width * percentage / 100);

            AnsiConsole.Markup($"[{color}]");
            AnsiConsole.Markup("[");
            AnsiConsole.Markup(new string('=', filledWidth));
            AnsiConsole.Markup(new string(' ', width - filledWidth));
            AnsiConsole.Markup("]");
            AnsiConsole.MarkupLine($"[/]");
        }

        /// <summary>
        /// Renders a final summary of the test run
        /// </summary>
        public void RenderSummary()
        {
            double successPercentage = (_completedTests > 0) ? (double)_succeededTests / _completedTests * 100 : 0;

            // Calculate average latency
            TimeSpan avgLatency = TimeSpan.Zero;
            if (_latencies.Count > 0)
            {
                avgLatency = TimeSpan.FromTicks((long)_latencies.Average(l => l.Ticks));
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Test Run Summary[/]");
            AnsiConsole.WriteLine($"Total Tests: {_totalTests}");
            AnsiConsole.WriteLine($"Completed: {_completedTests} ({(_completedTests / (double)_totalTests * 100):F1}%)");
            AnsiConsole.WriteLine($"Succeeded: {_succeededTests} ({successPercentage:F1}%)");
            AnsiConsole.WriteLine($"Failed: {_completedTests - _succeededTests}");
            AnsiConsole.WriteLine($"Average Latency: {avgLatency.TotalMilliseconds:F2}ms");
            AnsiConsole.WriteLine();
        }
    }
}