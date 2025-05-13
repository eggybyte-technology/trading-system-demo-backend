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
            report.AppendLine($"- Average latency: {stats.AverageLatencyMs:F2} ms");
            report.AppendLine($"- Requests per second: {stats.RequestsPerSecond:F2}");
            report.AppendLine();

            // Add latency analysis if we have results
            if (stats.Results.Count > 0)
            {
                report.AppendLine("Latency Analysis:");

                var successfulResults = stats.Results.Where(r => r.Success).ToList();

                if (successfulResults.Any())
                {
                    var minLatency = successfulResults.Min(r => r.LatencyMs);
                    var maxLatency = successfulResults.Max(r => r.LatencyMs);
                    var p50Latency = GetPercentileLatency(successfulResults, 50);
                    var p90Latency = GetPercentileLatency(successfulResults, 90);
                    var p95Latency = GetPercentileLatency(successfulResults, 95);
                    var p99Latency = GetPercentileLatency(successfulResults, 99);

                    report.AppendLine($"- Min latency: {minLatency} ms");
                    report.AppendLine($"- P50 latency: {p50Latency} ms");
                    report.AppendLine($"- P90 latency: {p90Latency} ms");
                    report.AppendLine($"- P95 latency: {p95Latency} ms");
                    report.AppendLine($"- P99 latency: {p99Latency} ms");
                    report.AppendLine($"- Max latency: {maxLatency} ms");

                    // Group results by operation type
                    var operationGroups = successfulResults.GroupBy(r => r.OperationType);
                    report.AppendLine();
                    report.AppendLine("Operation-specific Analysis:");

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
                        SuccessCount = stats.SuccessCount,
                        FailureCount = stats.FailureCount,
                        SuccessRate = stats.SuccessRate,
                        AverageLatencyMs = stats.AverageLatencyMs,
                        RequestsPerSecond = stats.RequestsPerSecond,
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