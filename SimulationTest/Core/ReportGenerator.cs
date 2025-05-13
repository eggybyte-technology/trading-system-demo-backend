using System.Text;
using System.Text.Json;

namespace SimulationTest.Core
{
    /// <summary>
    /// Generates detailed reports from test results
    /// </summary>
    public class ReportGenerator
    {
        private readonly TestLogger _logger;

        public ReportGenerator(TestLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate a detailed report for a stress test
        /// </summary>
        /// <param name="testDirectory">Directory to save the report</param>
        /// <param name="stats">Test statistics</param>
        /// <param name="userCount">Number of users created</param>
        /// <param name="ordersPerUser">Number of orders per user</param>
        public void GenerateStressTestReport(string testDirectory, TestStatistics stats, int userCount, int ordersPerUser)
        {
            var reportPath = Path.Combine(testDirectory, "stress_test_report.txt");
            var jsonReportPath = Path.Combine(testDirectory, "stress_test_data.json");

            // Generate summary report
            var report = new StringBuilder();
            report.AppendLine("=======================================================");
            report.AppendLine("||                STRESS TEST REPORT                 ||");
            report.AppendLine("=======================================================");
            report.AppendLine();
            report.AppendLine($"Test conducted at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Test duration: {stats.ElapsedTime:hh\\:mm\\:ss\\.fff}");
            report.AppendLine();
            report.AppendLine("Test Configuration:");
            report.AppendLine($"- Number of users: {userCount}");
            report.AppendLine($"- Orders per user: {ordersPerUser}");
            report.AppendLine($"- Total operations: {stats.TotalOperations}");
            report.AppendLine();
            report.AppendLine("Overall Results:");
            report.AppendLine($"- Success rate: {stats.SuccessRate:F2}%");
            report.AppendLine($"- Successful operations: {stats.SuccessCount}");
            report.AppendLine($"- Failed operations: {stats.FailureCount}");
            report.AppendLine();

            // Order-specific metrics (primary focus of the report)
            report.AppendLine("=======================================================");
            report.AppendLine("||              ORDER CREATION METRICS               ||");
            report.AppendLine("=======================================================");
            report.AppendLine($"- Order operations: {stats.OrderSuccessCount + stats.OrderFailureCount}");
            report.AppendLine($"- Successful orders: {stats.OrderSuccessCount}");
            report.AppendLine($"- Failed orders: {stats.OrderFailureCount}");
            report.AppendLine($"- Order success rate: {stats.OrderSuccessRate:F2}%");
            report.AppendLine($"- Orders per second: {stats.OrderRequestsPerSecond:F2}");
            report.AppendLine($"- Average order latency: {stats.OrderAverageLatencyMs:F2} ms");
            report.AppendLine();
            report.AppendLine("Order Latency Analysis:");
            report.AppendLine($"- Min order latency: {stats.OrderMinLatencyMs} ms");
            report.AppendLine($"- P50 order latency: {stats.OrderP50LatencyMs} ms");
            report.AppendLine($"- P90 order latency: {stats.OrderP90LatencyMs} ms");
            report.AppendLine($"- P95 order latency: {stats.OrderP95LatencyMs} ms");
            report.AppendLine($"- P99 order latency: {stats.OrderP99LatencyMs} ms");
            report.AppendLine($"- Max order latency: {stats.OrderMaxLatencyMs} ms");
            report.AppendLine();

            // Add latency analysis if we have results
            if (stats.Results.Count > 0)
            {
                report.AppendLine("Detailed Operation Analysis:");

                var successfulResults = stats.Results.Where(r => r.Success).ToList();

                // Group results by operation type
                var operationGroups = successfulResults.GroupBy(r => r.OperationType);

                foreach (var group in operationGroups)
                {
                    var avgLatency = group.Average(r => r.LatencyMs);
                    var opMaxLatency = group.Max(r => r.LatencyMs);
                    var opMinLatency = group.Min(r => r.LatencyMs);
                    var opCount = group.Count();
                    report.AppendLine($"- {group.Key}:");
                    report.AppendLine($"  * Count: {opCount}");
                    report.AppendLine($"  * Avg latency: {avgLatency:F2} ms");
                    report.AppendLine($"  * Min latency: {opMinLatency} ms");
                    report.AppendLine($"  * Max latency: {opMaxLatency} ms");
                }
            }

            // Write the report to file
            File.WriteAllText(reportPath, report.ToString());

            // Save detailed results to JSON for possible further analysis
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var jsonData = new
                {
                    TestConfiguration = new
                    {
                        UserCount = userCount,
                        OrdersPerUser = ordersPerUser,
                        TotalOperations = stats.TotalOperations,
                        TestTimestamp = DateTime.Now
                    },
                    TestResults = new
                    {
                        // Overall results
                        SuccessCount = stats.SuccessCount,
                        FailureCount = stats.FailureCount,
                        SuccessRate = stats.SuccessRate,
                        AverageLatencyMs = stats.AverageLatencyMs,
                        RequestsPerSecond = stats.RequestsPerSecond,
                        ElapsedTime = stats.ElapsedTime.ToString(),

                        // Order-specific results
                        OrderSuccessCount = stats.OrderSuccessCount,
                        OrderFailureCount = stats.OrderFailureCount,
                        OrderSuccessRate = stats.OrderSuccessRate,
                        OrderAverageLatencyMs = stats.OrderAverageLatencyMs,
                        OrderRequestsPerSecond = stats.OrderRequestsPerSecond,
                        OrderMinLatencyMs = stats.OrderMinLatencyMs,
                        OrderMaxLatencyMs = stats.OrderMaxLatencyMs,
                        OrderP50LatencyMs = stats.OrderP50LatencyMs,
                        OrderP90LatencyMs = stats.OrderP90LatencyMs,
                        OrderP95LatencyMs = stats.OrderP95LatencyMs,
                        OrderP99LatencyMs = stats.OrderP99LatencyMs,

                        // Detailed results
                        Results = stats.Results
                    }
                };

                string jsonString = JsonSerializer.Serialize(jsonData, jsonOptions);
                File.WriteAllText(jsonReportPath, jsonString);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save JSON report: {ex.Message}");
            }

            _logger.Info($"Reports saved to: {testDirectory}");
        }

        /// <summary>
        /// Generate a detailed report for a unit test
        /// </summary>
        /// <param name="testDirectory">Directory to save the report</param>
        /// <param name="stats">Test statistics</param>
        public void GenerateUnitTestReport(string testDirectory, TestStatistics stats)
        {
            var reportPath = Path.Combine(testDirectory, "unit_test_report.txt");
            var jsonReportPath = Path.Combine(testDirectory, "unit_test_data.json");

            // Generate summary report
            var report = new StringBuilder();
            report.AppendLine("=======================================================");
            report.AppendLine("||                 UNIT TEST REPORT                  ||");
            report.AppendLine("=======================================================");
            report.AppendLine();
            report.AppendLine($"Test conducted at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Test duration: {stats.ElapsedTime:hh\\:mm\\:ss\\.fff}");
            report.AppendLine();
            report.AppendLine("Overall Results:");
            report.AppendLine($"- Success rate: {stats.SuccessRate:F2}%");
            report.AppendLine($"- Successful operations: {stats.SuccessCount}");
            report.AppendLine($"- Failed operations: {stats.FailureCount}");
            report.AppendLine($"- Average latency: {stats.AverageLatencyMs:F2} ms");
            report.AppendLine();

            // Add per-operation results
            if (stats.Results.Count > 0)
            {
                report.AppendLine("Operation Results:");
                report.AppendLine("--------------------------------------------------------------------------------------------------");
                report.AppendLine("| Operation                        | Status  | Latency (ms) | Timestamp           |");
                report.AppendLine("--------------------------------------------------------------------------------------------------");

                foreach (var result in stats.Results)
                {
                    string status = result.Success ? "SUCCESS" : "FAILED ";
                    string operation = result.OperationType;
                    if (operation.Length > 32)
                        operation = operation.Substring(0, 29) + "...";
                    else
                        operation = operation.PadRight(32);

                    report.AppendLine($"| {operation} | {status} | {result.LatencyMs,11} | {result.Timestamp:yyyy-MM-dd HH:mm:ss} |");
                }

                report.AppendLine("--------------------------------------------------------------------------------------------------");
            }

            // Write the report to file
            File.WriteAllText(reportPath, report.ToString());

            // Save detailed results to JSON for possible further analysis
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var jsonData = new
                {
                    TestResults = new
                    {
                        SuccessCount = stats.SuccessCount,
                        FailureCount = stats.FailureCount,
                        SuccessRate = stats.SuccessRate,
                        AverageLatencyMs = stats.AverageLatencyMs,
                        ElapsedTime = stats.ElapsedTime.ToString(),
                        Results = stats.Results
                    }
                };

                string jsonString = JsonSerializer.Serialize(jsonData, jsonOptions);
                File.WriteAllText(jsonReportPath, jsonString);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save JSON report: {ex.Message}");
            }

            _logger.Info($"Reports saved to: {testDirectory}");
        }

        /// <summary>
        /// Calculate the percentile latency for a set of results
        /// </summary>
        private long GetPercentileLatency(List<OperationResult> results, int percentile)
        {
            if (results == null || !results.Any())
                return 0;

            var sortedLatencies = results.Select(r => r.LatencyMs).OrderBy(l => l).ToList();

            var index = (int)Math.Ceiling((percentile / 100.0) * sortedLatencies.Count) - 1;
            if (index < 0) index = 0;

            return sortedLatencies[index];
        }
    }
}