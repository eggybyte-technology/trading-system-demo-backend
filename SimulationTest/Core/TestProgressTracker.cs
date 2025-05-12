using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;

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
        /// <param name="totalTests">The total number of tests that will be run</param>
        /// <param name="refreshIntervalMs">The refresh interval in milliseconds</param>
        public TestProgressTracker(int totalTests, int refreshIntervalMs = 500)
        {
            _totalTests = totalTests;
            _refreshIntervalMs = refreshIntervalMs;
        }

        /// <summary>
        /// Gets all latency measurements collected during test execution
        /// </summary>
        /// <returns>A collection of latency measurements</returns>
        public IEnumerable<TimeSpan> GetLatencies()
        {
            return _latencies.ToArray();
        }

        /// <summary>
        /// Starts tracking test progress
        /// </summary>
        public void StartTracking()
        {
            lock (_lock)
            {
                if (_isDisplaying)
                    return;

                _isDisplaying = true;
                _refreshTimer = new Timer(UpdateDisplay, null, 0, _refreshIntervalMs);
            }
        }

        /// <summary>
        /// Updates the console display with the current progress
        /// </summary>
        private void UpdateDisplay(object state)
        {
            if (!_isDisplaying)
                return;

            double percentComplete = _totalTests > 0
                ? Math.Round(100.0 * _completedTests / _totalTests, 1)
                : 0;

            var avgLatency = _latencies.Count > 0
                ? _latencies.Average(l => l.TotalMilliseconds)
                : 0;

            // Use explicit markup to avoid parsing errors
            AnsiConsole.MarkupLine($"Progress: {_completedTests}/{_totalTests} ([green]{percentComplete}%[/])");
        }

        /// <summary>
        /// Updates the test results with a new test result
        /// </summary>
        /// <param name="result">The test result to add</param>
        public void UpdateTestResult(ApiTestResult result)
        {
            lock (_lock)
            {
                _completedTests++;
                if (result.Success)
                    _succeededTests++;

                _latencies.Add(result.Duration);
            }
        }

        /// <summary>
        /// Adds a latency measurement from an external source
        /// </summary>
        /// <param name="latency">The latency measurement to add</param>
        public void AddLatency(TimeSpan latency)
        {
            lock (_lock)
            {
                _latencies.Add(latency);
            }
        }

        /// <summary>
        /// Updates the test counts directly
        /// </summary>
        /// <param name="completed">Number of completed tests</param>
        /// <param name="succeeded">Number of succeeded tests</param>
        public void UpdateTestCounts(int completed, int succeeded)
        {
            lock (_lock)
            {
                // If the completed test count from the running results is different,
                // update our total based on the actual results
                if (completed != _totalTests && completed > 0)
                {
                    _totalTests = completed;
                }

                _completedTests = completed;
                _succeededTests = succeeded;
            }
        }

        /// <summary>
        /// Stops tracking test progress
        /// </summary>
        public void StopTracking()
        {
            lock (_lock)
            {
                if (!_isDisplaying)
                    return;

                _isDisplaying = false;
                _refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _refreshTimer?.Dispose();
                _refreshTimer = null;
            }
        }

        /// <summary>
        /// Renders a summary of the test results
        /// </summary>
        public void RenderSummary()
        {
            lock (_lock)
            {
                // Get average latency in milliseconds
                var avgLatency = _latencies.Count > 0
                    ? _latencies.Average(l => l.TotalMilliseconds)
                    : 0;

                // Calculate percentage
                double percentComplete = _totalTests > 0
                    ? Math.Round(100.0 * _completedTests / _totalTests, 1)
                    : 0;

                // Calculate succeeded percentage
                double succeededPercentage = _completedTests > 0
                    ? Math.Round(100.0 * _succeededTests / _completedTests, 1)
                    : 0;

                // Calculate failed tests
                int failedTests = _completedTests - _succeededTests;

                // Create a table
                var table = new Table();
                table.Title = new TableTitle("Test Run Summary", new Style(Color.Yellow));
                table.Border(TableBorder.Rounded);

                // Add columns
                table.AddColumn("Total Tests");
                table.AddColumn("Completed");
                table.AddColumn("Succeeded");
                table.AddColumn("Failed");
                table.AddColumn("Average Latency");

                // Add a row with the summary data
                table.AddRow(
                    _totalTests.ToString(),
                    $"{_completedTests} ({percentComplete}%)",
                    $"{_succeededTests} ({succeededPercentage}%)",
                    failedTests.ToString(),
                    $"{avgLatency:F2}ms"
                );

                // Render the table
                AnsiConsole.WriteLine();
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
        }
    }
}