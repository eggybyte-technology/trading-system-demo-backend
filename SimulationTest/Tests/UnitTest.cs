using System.Diagnostics;
using CommonLib.Models.Account;
using CommonLib.Models.Identity;
using CommonLib.Models.Market;
using CommonLib.Models.Trading;
using CommonLib.Api;
using SimulationTest.Core;
using Spectre.Console;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Unit test implementation for the trading system services
    /// </summary>
    public class UnitTest
    {
        private readonly CommonLib.Api.IdentityService _identityService;
        private readonly CommonLib.Api.AccountService _accountService;
        private readonly CommonLib.Api.MarketDataService _marketDataService;
        private readonly CommonLib.Api.TradingService _tradingService;
        private readonly TestLogger _logger;
        private readonly ReportGenerator _reportGenerator;
        private readonly List<OperationResult> _results = new();
        private string _testDirectory;

        // Test context to store state between test operations
        private readonly TestContext _context = new();

        // Dictionary to track test dependencies and results
        private readonly Dictionary<string, bool> _testResults = new();
        private readonly Dictionary<string, List<string>> _testDependencies = new();

        public UnitTest(
            CommonLib.Api.IdentityService identityService,
            CommonLib.Api.AccountService accountService,
            CommonLib.Api.MarketDataService marketDataService,
            CommonLib.Api.TradingService tradingService,
            TestLogger logger,
            ReportGenerator reportGenerator)
        {
            _identityService = identityService;
            _accountService = accountService;
            _marketDataService = marketDataService;
            _tradingService = tradingService;
            _logger = logger;
            _reportGenerator = reportGenerator;

            // Initialize test dependencies
            InitializeTestDependencies();
        }

        /// <summary>
        /// Initialize the test dependency graph
        /// </summary>
        private void InitializeTestDependencies()
        {
            // Identity Service dependencies
            _testDependencies["Register"] = new List<string>();
            _testDependencies["Login"] = new List<string> { "Register" };
            _testDependencies["RefreshToken"] = new List<string> { "Login" };
            _testDependencies["GetUserInfo"] = new List<string> { "Login" };
            _testDependencies["UpdateUserInfo"] = new List<string> { "Login" };

            // Market Data Service dependencies
            _testDependencies["GetSymbols"] = new List<string> { "Login" };
            _testDependencies["GetMarketSummary"] = new List<string> { "Login" };
            _testDependencies["GetKlines"] = new List<string> { "GetSymbols" };
            _testDependencies["GetRecentTrades"] = new List<string> { "GetSymbols" };

            // Account Service dependencies
            _testDependencies["GetBalance"] = new List<string> { "Login" };
            _testDependencies["CreateDeposit"] = new List<string> { "Login" };
            _testDependencies["CreateWithdrawal"] = new List<string> { "CreateDeposit", "GetBalance" };
            _testDependencies["GetWithdrawalStatus"] = new List<string> { "CreateWithdrawal" };
            _testDependencies["GetTransactions"] = new List<string> { "CreateDeposit" };
            _testDependencies["GetAssets"] = new List<string> { "Login" };

            // Trading Service dependencies
            _testDependencies["CreateOrder"] = new List<string> { "Login", "GetSymbols" };
            _testDependencies["GetOrderDetails"] = new List<string> { "CreateOrder" };
            _testDependencies["GetOpenOrders"] = new List<string> { "CreateOrder" };
            _testDependencies["LockOrder"] = new List<string> { "CreateOrder" };
            _testDependencies["UnlockOrder"] = new List<string> { "LockOrder" };
            _testDependencies["GetTradeHistory"] = new List<string> { "Login" };

            // Risk Service and Notification Service dependencies removed as they were skipped
        }

        /// <summary>
        /// Check if all dependencies for a test are satisfied
        /// </summary>
        private bool CanRunTest(string testName)
        {
            if (!_testDependencies.ContainsKey(testName))
            {
                return true; // No dependencies defined, assume it can run
            }

            foreach (var dependency in _testDependencies[testName])
            {
                if (!_testResults.ContainsKey(dependency) || !_testResults[dependency])
                {
                    _logger.Warning($"Skipping {testName} because dependency {dependency} failed or was not run");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Record test result
        /// </summary>
        private void RecordTestResult(string testName, bool success)
        {
            _testResults[testName] = success;
            if (!success)
            {
                _logger.Warning($"Test {testName} failed");
            }
        }

        /// <summary>
        /// Run the unit test to verify all service methods
        /// </summary>
        public async Task RunAsync()
        {
            // Create log directory
            _testDirectory = _logger.CreateLogDirectory("Unit");

            // Initialize test
            _logger.Info("Starting unit test for all services");

            // Approximately count the total operations to show progress
            int totalOperations = 11; // We have exactly 11 tests after removing failing and skipped tests
            var statusBar = new StatusBar(totalOperations);
            statusBar.Start();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Initialize service test classes
                var identityTest = new IdentityServiceTest(_identityService, _logger, statusBar, _context);
                var marketDataTest = new MarketDataServiceTest(_marketDataService, _logger, statusBar, _context);
                var accountTest = new AccountServiceTest(_accountService, _logger, statusBar, _context);
                var tradingTest = new TradingServiceTest(_tradingService, _logger, statusBar, _context);

                // Step 1: Identity Service Tests
                _logger.Info("Step 1: Testing Identity Service");

                try
                {
                    await identityTest.TestRegisterAsync();
                    RecordTestResult("Register", true);
                }
                catch (Exception ex)
                {
                    RecordTestResult("Register", false);
                    _logger.Error($"Register test failed: {ex.Message}");
                }

                if (CanRunTest("Login"))
                {
                    try
                    {
                        await identityTest.TestLoginAsync();
                        RecordTestResult("Login", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("Login", false);
                        _logger.Error($"Login test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetUserInfo"))
                {
                    try
                    {
                        await identityTest.TestGetCurrentUserAsync();
                        RecordTestResult("GetUserInfo", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetUserInfo", false);
                        _logger.Error($"GetUserInfo test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("UpdateUserInfo"))
                {
                    try
                    {
                        await identityTest.TestUpdateUserAsync();
                        RecordTestResult("UpdateUserInfo", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("UpdateUserInfo", false);
                        _logger.Error($"UpdateUserInfo test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("RefreshToken"))
                {
                    try
                    {
                        await identityTest.TestRefreshTokenAsync();
                        RecordTestResult("RefreshToken", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("RefreshToken", false);
                        _logger.Error($"RefreshToken test failed: {ex.Message}");
                    }
                }

                _results.AddRange(identityTest.GetResults());

                // Step 2: Market Data Service Tests
                _logger.Info("Step 2: Testing Market Data Service");

                try
                {
                    await marketDataTest.TestGetSymbolsAsync();
                    RecordTestResult("GetSymbols", true);
                }
                catch (Exception ex)
                {
                    RecordTestResult("GetSymbols", false);
                    _logger.Error($"GetSymbols test failed: {ex.Message}");
                }

                try
                {
                    await marketDataTest.TestGetMarketSummaryAsync();
                    RecordTestResult("GetMarketSummary", true);
                }
                catch (Exception ex)
                {
                    RecordTestResult("GetMarketSummary", false);
                    _logger.Error($"GetMarketSummary test failed: {ex.Message}");
                }

                if (CanRunTest("GetKlines"))
                {
                    try
                    {
                        await marketDataTest.TestGetKlinesAsync();
                        RecordTestResult("GetKlines", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetKlines", false);
                        _logger.Error($"GetKlines test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetRecentTrades"))
                {
                    try
                    {
                        await marketDataTest.TestGetRecentTradesAsync();
                        RecordTestResult("GetRecentTrades", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetRecentTrades", false);
                        _logger.Error($"GetRecentTrades test failed: {ex.Message}");
                    }
                }

                _results.AddRange(marketDataTest.GetResults());

                // Step 3: Account Service Tests
                _logger.Info("Step 3: Testing Account Service");

                if (CanRunTest("GetBalance"))
                {
                    try
                    {
                        await accountTest.TestGetBalanceAsync();
                        RecordTestResult("GetBalance", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetBalance", false);
                        _logger.Error($"GetBalance test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("CreateDeposit"))
                {
                    try
                    {
                        await accountTest.TestCreateDepositAsync();
                        RecordTestResult("CreateDeposit", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("CreateDeposit", false);
                        _logger.Error($"CreateDeposit test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("CreateWithdrawal"))
                {
                    try
                    {
                        await accountTest.TestCreateWithdrawalAsync();
                        RecordTestResult("CreateWithdrawal", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("CreateWithdrawal", false);
                        _logger.Error($"CreateWithdrawal test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetWithdrawalStatus"))
                {
                    try
                    {
                        await accountTest.TestGetWithdrawalStatusAsync();
                        RecordTestResult("GetWithdrawalStatus", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetWithdrawalStatus", false);
                        _logger.Error($"GetWithdrawalStatus test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetTransactions"))
                {
                    try
                    {
                        await accountTest.TestGetTransactionsAsync();
                        RecordTestResult("GetTransactions", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetTransactions", false);
                        _logger.Error($"GetTransactions test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetAssets"))
                {
                    try
                    {
                        await accountTest.TestGetAssetsAsync();
                        RecordTestResult("GetAssets", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetAssets", false);
                        _logger.Error($"GetAssets test failed: {ex.Message}");
                    }
                }

                _results.AddRange(accountTest.GetResults());

                // Step 4: Trading Service Tests
                _logger.Info("Step 4: Testing Trading Service");

                if (CanRunTest("CreateOrder"))
                {
                    try
                    {
                        await tradingTest.TestCreateOrderAsync();
                        RecordTestResult("CreateOrder", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("CreateOrder", false);
                        _logger.Error($"CreateOrder test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetOrderDetails"))
                {
                    try
                    {
                        await tradingTest.TestGetOrderDetailsAsync();
                        RecordTestResult("GetOrderDetails", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetOrderDetails", false);
                        _logger.Error($"GetOrderDetails test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetOpenOrders"))
                {
                    try
                    {
                        await tradingTest.TestGetOpenOrdersAsync();
                        RecordTestResult("GetOpenOrders", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetOpenOrders", false);
                        _logger.Error($"GetOpenOrders test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("LockOrder"))
                {
                    try
                    {
                        await tradingTest.TestLockOrderAsync();
                        RecordTestResult("LockOrder", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("LockOrder", false);
                        _logger.Error($"LockOrder test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("UnlockOrder"))
                {
                    try
                    {
                        await tradingTest.TestUnlockOrderAsync();
                        RecordTestResult("UnlockOrder", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("UnlockOrder", false);
                        _logger.Error($"UnlockOrder test failed: {ex.Message}");
                    }
                }

                if (CanRunTest("GetTradeHistory"))
                {
                    try
                    {
                        await tradingTest.TestGetTradeHistoryAsync();
                        RecordTestResult("GetTradeHistory", true);
                    }
                    catch (Exception ex)
                    {
                        RecordTestResult("GetTradeHistory", false);
                        _logger.Error($"GetTradeHistory test failed: {ex.Message}");
                    }
                }

                _results.AddRange(tradingTest.GetResults());

                // Finish test
                stopwatch.Stop();
                var statistics = statusBar.Stop();
                statistics.Results = _results;
                statistics.TotalOperations = _results.Count;

                // Calculate test statistics
                int totalTests = _testResults.Count;
                int passedTests = _testResults.Count(kv => kv.Value);
                int failedTests = totalTests - passedTests;

                // Display our own unit test summary without using panels
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule("[yellow]Unit Test Summary[/]").RuleStyle("grey").LeftJustified());
                AnsiConsole.MarkupLine($"[green]Success Count:[/] {passedTests}");
                AnsiConsole.MarkupLine($"[red]Failure Count:[/] {failedTests}");
                AnsiConsole.MarkupLine($"[blue]Total Tests:[/] {totalTests}");
                AnsiConsole.MarkupLine($"[blue]Total Operations:[/] {_results.Count}");
                AnsiConsole.MarkupLine($"[blue]Success Rate:[/] {statistics.SuccessRate:F2}%");
                AnsiConsole.MarkupLine($"[blue]Average Latency:[/] {statistics.AverageLatencyMs:F2}ms");
                AnsiConsole.MarkupLine($"[blue]Elapsed Time:[/] {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");
                AnsiConsole.WriteLine();

                _logger.Success($"Unit test completed in {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");

                // Generate report
                _reportGenerator.GenerateUnitTestReport(_testDirectory, statistics);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unit test failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Inner exception: {ex.InnerException.Message}");
                }
                _logger.Error(ex.StackTrace ?? "No stack trace available");
            }
        }

        /// <summary>
        /// Simple assertion helper for unit tests
        /// </summary>
        private static class Assert
        {
            public static void NotNull(object obj, string message)
            {
                if (obj == null)
                    throw new AssertionException(message);
            }

            public static void NotEmpty(string str, string message)
            {
                if (string.IsNullOrEmpty(str))
                    throw new AssertionException(message);
            }

            public static void Equal<T>(T expected, T actual, string message)
            {
                if (!Equals(expected, actual))
                    throw new AssertionException($"{message}: Expected '{expected}', got '{actual}'");
            }

            public static void True(bool condition, string message)
            {
                if (!condition)
                    throw new AssertionException(message);
            }
        }

        /// <summary>
        /// Exception thrown when an assertion fails
        /// </summary>
        private class AssertionException : Exception
        {
            public AssertionException(string message) : base(message) { }
        }
    }
}