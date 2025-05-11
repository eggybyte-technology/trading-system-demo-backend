using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SimulationTest.Helpers;
using Spectre.Console;

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

        /// <summary>
        /// Initializes a new instance of the IntegratedTestWorkflow class
        /// </summary>
        /// <param name="configuration">Configuration for the test workflow</param>
        public IntegratedTestWorkflow(IConfiguration configuration)
        {
            _configuration = configuration;

            // Define service URLs
            _serviceUrls = new Dictionary<string, string>
            {
                { "identity", _configuration["SimulationSettings:IdentityHost"] ?? "http://identity.trading-system.local" },
                { "trading", _configuration["SimulationSettings:TradingHost"] ?? "http://trading.trading-system.local" },
                { "market-data", _configuration["SimulationSettings:MarketDataHost"] ?? "http://market-data.trading-system.local" },
                { "account", _configuration["SimulationSettings:AccountHost"] ?? "http://account.trading-system.local" },
                { "risk", _configuration["SimulationSettings:RiskHost"] ?? "http://risk.trading-system.local" },
                { "notification", _configuration["SimulationSettings:NotificationHost"] ?? "http://notification.trading-system.local" },
                { "match-making", _configuration["SimulationSettings:MatchMakingHost"] ?? "http://match-making.trading-system.local" }
            };

            // Define the test order by service dependency
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
                
                // Identity flow
                "SimulationTest.Tests.IdentityServiceTests.Register_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken",
                "SimulationTest.Tests.IdentityServiceTests.GetCurrentUser_WhenAuthenticated_ShouldReturnUserInfo",
                "SimulationTest.Tests.IdentityServiceTests.UpdateUser_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.IdentityServiceTests.RefreshToken_WithValidToken_ShouldReturnNewToken",
                
                // Account tests
                "SimulationTest.Tests.AccountServiceTests.GetBalances_WhenAuthenticated_ShouldReturnBalances",
                "SimulationTest.Tests.AccountServiceTests.CreateDeposit_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.AccountServiceTests.GetTransactions_WhenAuthenticated_ShouldReturnTransactions",
                "SimulationTest.Tests.AccountServiceTests.CreateWithdrawal_WithValidData_ShouldSucceed",
                "SimulationTest.Tests.AccountServiceTests.GetWithdrawalStatus_WithValidId_ShouldReturnWithdrawal",
                "SimulationTest.Tests.AccountServiceTests.GetAssets_WhenAuthenticated_ShouldReturnAssets",
                
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
                "SimulationTest.Tests.TradingServiceTests.CancelOrder_WithValidOrderId_ShouldSucceed",
                "SimulationTest.Tests.TradingServiceTests.GetOrderHistory_WhenAuthenticated_ShouldReturnOrderHistory",
                "SimulationTest.Tests.TradingServiceTests.GetTradeHistory_WhenAuthenticated_ShouldReturnTradeHistory",
                
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

            // Initialize progress tracker with total number of tests
            _progressTracker = new TestProgressTracker(_testOrder.Count);
        }

        /// <summary>
        /// Runs the integrated workflow of tests in sequence
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public async Task<bool> RunWorkflowAsync()
        {
            AnsiConsole.MarkupLine("[green]Starting integrated test workflow for Trading System API...[/]");

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
                AnsiConsole.MarkupLine($"[yellow]Warning: Service '{result.Key}' is unavailable. Some tests will be skipped.[/]");
            }

            // Create a test runner for the ordered tests
            var testRunner = new UnitTestRunner(_configuration);

            // Start tracking progress
            _progressTracker.StartTracking();

            // Run the tests in the defined order
            bool allTestsPassed = true;

            try
            {
                await testRunner.RunTestsAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error running test workflow: {ex.Message}[/]");
                allTestsPassed = false;
            }
            finally
            {
                // Stop tracking progress
                _progressTracker.StopTracking();
            }

            // Display a final summary
            _progressTracker.RenderSummary();

            return allTestsPassed;
        }
    }
}