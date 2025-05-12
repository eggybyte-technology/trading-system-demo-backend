using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SimulationTest.Helpers;
using Spectre.Console;
using System.IO;

namespace SimulationTest.Core
{
    /// <summary>
    /// Manages the integrated test workflow across all services
    /// </summary>
    public class IntegratedTestWorkflow
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _serviceUrls;
        private readonly List<string> _testOrder;
        private readonly TestProgressTracker _progressTracker;
        private readonly string _logFilePath;

        /// <summary>
        /// Initializes a new instance of the IntegratedTestWorkflow class
        /// </summary>
        /// <param name="configuration">Configuration for the test workflow</param>
        public IntegratedTestWorkflow(IConfiguration configuration)
        {
            _configuration = configuration;

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Set up workflow log file
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine("logs", $"workflow_log_{timestamp}.txt");

            // Initialize the log file
            using var logWriter = new StreamWriter(_logFilePath, false);
            logWriter.WriteLine("=== Trading System Test Workflow Log ===");
            logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine("=================================\n");

            // Define service URLs
            _serviceUrls = new Dictionary<string, string>
            {
                { "identity", _configuration["Services:IdentityHost"] ?? "http://identity.trading-system.local" },
                { "trading", _configuration["Services:TradingHost"] ?? "http://trading.trading-system.local" },
                { "market-data", _configuration["Services:MarketDataHost"] ?? "http://market-data.trading-system.local" },
                { "account", _configuration["Services:AccountHost"] ?? "http://account.trading-system.local" },
                { "risk", _configuration["Services:RiskHost"] ?? "http://risk.trading-system.local" },
                { "notification", _configuration["Services:NotificationHost"] ?? "http://notification.trading-system.local" },
                { "match-making", _configuration["Services:MatchMakingHost"] ?? "http://match-making.trading-system.local" }
            };

            // Write service URLs to log
            logWriter.WriteLine("=== Service URLs ===");
            foreach (var service in _serviceUrls)
            {
                logWriter.WriteLine($"{service.Key}: {service.Value}");
            }
            logWriter.WriteLine();

            // Define the test order by service dependency - make sure identity tests are first!
            _testOrder = new List<string>
            {
                // Connectivity checks first
                "SimulationTest.Tests.IdentityServiceTests.CheckConnectivity_IdentityService_ShouldBeAccessible",
                "SimulationTest.Tests.MarketDataServiceTests.CheckConnectivity_MarketDataService_ShouldBeAccessible",
                "SimulationTest.Tests.TradingServiceTests.CheckConnectivity_TradingService_ShouldBeAccessible",
                "SimulationTest.Tests.AccountServiceTests.CheckConnectivity_AccountService_ShouldBeAccessible",
                "SimulationTest.Tests.RiskServiceTests.CheckConnectivity_RiskService_ShouldBeAccessible",
                "SimulationTest.Tests.NotificationServiceTests.CheckConnectivity_NotificationService_ShouldBeAccessible",
                "SimulationTest.Tests.MatchMakingServiceTests.CheckConnectivity_MatchMakingService_ShouldBeAccessible",
                
                // Identity flow comes next - CRITICAL for other tests
                "SimulationTest.Tests.IdentityServiceTests.Register_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken",
                "SimulationTest.Tests.IdentityServiceTests.GetCurrentUser_WhenAuthenticated_ShouldReturnUserInfo",
                
                // Once we have authentication, we can do other tests
                // Account tests
                "SimulationTest.Tests.AccountServiceTests.GetBalances_WhenAuthenticated_ShouldReturnBalances",
                "SimulationTest.Tests.AccountServiceTests.CreateDeposit_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.AccountServiceTests.GetTransactions_WhenAuthenticated_ShouldReturnTransactions",
                
                // Market Data tests
                "SimulationTest.Tests.MarketDataServiceTests.GetSymbols_ShouldReturnSymbols",
                "SimulationTest.Tests.MarketDataServiceTests.GetTicker_WithValidSymbol_ShouldReturnMarketData",
                "SimulationTest.Tests.MarketDataServiceTests.GetMarketSummary_ShouldReturnSummary",
                "SimulationTest.Tests.MarketDataServiceTests.GetOrderBookDepth_WithValidSymbol_ShouldReturnOrderBook",
                "SimulationTest.Tests.MarketDataServiceTests.GetKlines_WithValidParameters_ShouldReturnKlines",
                "SimulationTest.Tests.MarketDataServiceTests.GetRecentTrades_WithValidSymbol_ShouldReturnTrades",
                
                // Trading tests
                "SimulationTest.Tests.TradingServiceTests.CreateOrder_WithValidData_ShouldReturnOrder",
                "SimulationTest.Tests.TradingServiceTests.GetOrder_WithValidOrderId_ShouldReturnOrder",
                "SimulationTest.Tests.TradingServiceTests.GetOpenOrders_WhenAuthenticated_ShouldReturnOpenOrders",
                
                // Other account tests that require trading
                "SimulationTest.Tests.AccountServiceTests.CreateWithdrawal_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.AccountServiceTests.GetWithdrawalStatus_WithValidId_ShouldReturnWithdrawal",
                "SimulationTest.Tests.AccountServiceTests.GetAssets_WhenAuthenticated_ShouldReturnAssets",
                
                // Remaining trading tests
                "SimulationTest.Tests.TradingServiceTests.CancelOrder_WithValidOrderId_ShouldSucceed",
                "SimulationTest.Tests.TradingServiceTests.GetOrderHistory_WhenAuthenticated_ShouldReturnOrderHistory",
                "SimulationTest.Tests.TradingServiceTests.GetTradeHistory_WhenAuthenticated_ShouldReturnTradeHistory",
                
                // Final identity tests
                "SimulationTest.Tests.IdentityServiceTests.UpdateUser_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.IdentityServiceTests.RefreshToken_WithValidToken_ShouldReturnNewToken",
                
                // Risk tests
                "SimulationTest.Tests.RiskServiceTests.GetRiskStatus_WhenAuthenticated_ShouldReturnRiskProfile",
                "SimulationTest.Tests.RiskServiceTests.GetTradingLimits_WhenAuthenticated_ShouldReturnRiskRules",
                "SimulationTest.Tests.RiskServiceTests.AcknowledgeRiskAlert_WithValidId_ShouldSucceed",
                
                // Notification tests
                "SimulationTest.Tests.NotificationServiceTests.GetNotifications_WhenAuthenticated_ShouldReturnNotifications",
                "SimulationTest.Tests.NotificationServiceTests.UpdateNotificationSettings_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.NotificationServiceTests.MarkNotificationAsRead_WithValidId_ShouldSucceed",
                "SimulationTest.Tests.NotificationServiceTests.WebSocketConnection_ShouldEstablishConnection",
                
                // Match Making tests
                "SimulationTest.Tests.MatchMakingServiceTests.GetMatchEngineStatus_WhenAuthenticated_ShouldReturnStatus",
                "SimulationTest.Tests.MatchMakingServiceTests.GetMatchingJobsHistory_WhenAuthenticated_ShouldReturnJobs"
            };

            // Write test order to log
            logWriter.WriteLine("=== Test Order ===");
            for (int i = 0; i < _testOrder.Count; i++)
            {
                logWriter.WriteLine($"{i + 1}. {_testOrder[i]}");
            }
            logWriter.WriteLine();

            // Initialize progress tracker with total number of tests
            _progressTracker = new TestProgressTracker(_testOrder.Count);
        }

        /// <summary>
        /// Logs a message to the workflow log file and console
        /// </summary>
        private void LogMessage(string message, bool isError = false)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string fullMessage = $"[{timestamp}] {message}";

            // Log to file
            using (var writer = new StreamWriter(_logFilePath, true))
            {
                writer.WriteLine(fullMessage);
            }

            // Use Markup.Escape to properly handle all markup special characters
            string escapedMessage = Markup.Escape(fullMessage);

            if (isError)
                AnsiConsole.MarkupLine($"[red]{escapedMessage}[/]");
            else
                AnsiConsole.MarkupLine($"[blue]{escapedMessage}[/]");
        }

        /// <summary>
        /// Runs the integrated workflow of tests in sequence
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public async Task<bool> RunWorkflowAsync()
        {
            LogMessage("Starting integrated test workflow for Trading System API...");

            // First check connectivity to all services
            var httpClientFactory = new HttpClientFactory();
            httpClientFactory.Configure(
                int.Parse(_configuration["TestSettings:TestTimeout"] ?? "30"));
            httpClientFactory.ConfigureServiceUrls(_serviceUrls);

            var connectivityChecker = new ServiceConnectivityChecker(httpClientFactory, _serviceUrls);
            var connectivityResults = await connectivityChecker.CheckAllServicesAsync();

            // If any service is down, report it but continue with available services
            foreach (var result in connectivityResults.Where(r => !r.Value))
            {
                LogMessage($"Warning: Service '{result.Key}' is unavailable. Some tests will be skipped.", true);
            }

            // Create a test runner for the ordered tests
            var testRunner = new UnitTestRunner(_configuration);

            // Start tracking progress
            _progressTracker.StartTracking();

            // Run the tests in the defined order
            bool allTestsPassed = true;
            int testsRun = 0;
            int testsPassed = 0;
            int testsFailed = 0;
            int testsSkipped = 0;

            try
            {
                var results = await testRunner.RunTestsAsync();

                // Use the TestRunResults instead of calculating our own values
                if (results != null)
                {
                    testsRun = results.Total;
                    testsPassed = results.Passed;
                    testsFailed = results.Failed;
                    testsSkipped = results.Skipped;

                    // Update the progress tracker with the actual results and latency data
                    _progressTracker.UpdateTestCounts(testsRun, testsPassed);

                    // Transfer the latency data from the test runner to the progress tracker
                    if (results.TestLatencies != null && results.TestLatencies.Count > 0)
                    {
                        foreach (var latency in results.TestLatencies)
                        {
                            _progressTracker.AddLatency(latency);
                        }
                    }

                    // Check if any tests failed
                    allTestsPassed = testsFailed == 0;
                }

                LogMessage($"Test run completed with {testsRun} result(s)");

                // Check for critical failure and report it - identity tests MUST pass
                if (testsFailed > 0 && testsRun < 10)
                {
                    LogMessage("CRITICAL ERROR: Initial identity tests failed. Subsequent tests will likely fail.", true);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error running test workflow: {ex.Message}", true);
                allTestsPassed = false;
            }
            finally
            {
                // Stop tracking progress
                _progressTracker.StopTracking();
            }

            // Display a final summary
            _progressTracker.RenderSummary();

            LogMessage($"Test workflow complete. Tests run: {testsRun}, Passed: {testsPassed}, Failed: {testsFailed}" + (testsSkipped > 0 ? $", Skipped: {testsSkipped}" : ""));

            return allTestsPassed;
        }
    }
}