using System;

namespace SimulationTest.Core
{
    /// <summary>
    /// Represents progress information for both unit tests and stress tests
    /// </summary>
    public class TestProgress
    {
        /// <summary>
        /// Gets or sets the progress message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the percentage of completion (0-100)
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// Gets or sets the number of completed items
        /// </summary>
        public int Completed { get; set; }

        /// <summary>
        /// Gets or sets the total number of items
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the number of passed tests (unit tests only)
        /// </summary>
        public int Passed { get; set; } = -1;

        /// <summary>
        /// Gets or sets the number of failed tests (unit tests only)
        /// </summary>
        public int Failed { get; set; } = -1;

        /// <summary>
        /// Gets or sets the number of skipped tests (unit tests only)
        /// </summary>
        public int Skipped { get; set; } = -1;

        /// <summary>
        /// Gets or sets the average latency in milliseconds (stress tests primarily)
        /// </summary>
        public double AverageLatency { get; set; } = -1;

        /// <summary>
        /// Gets or sets the success rate percentage (stress tests primarily)
        /// </summary>
        public double SuccessRate { get; set; } = -1;

        /// <summary>
        /// Gets or sets the operations per second (stress tests primarily)
        /// </summary>
        public double OperationsPerSecond { get; set; } = -1;

        /// <summary>
        /// Gets or sets a log message for detailed status information
        /// </summary>
        public string LogMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a timestamp for the progress update
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets whether this is a final update
        /// </summary>
        public bool IsFinal { get; set; }
    }
}