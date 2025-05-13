using System.Diagnostics;
using CommonLib.Models.Identity;
using CommonLib.Models.Trading;
using SimulationTest.Core;
using CommonLib.Api;
using Spectre.Console;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Enums for trading orders
    /// </summary>
    public enum OrderSide
    {
        BUY,
        SELL
    }

    public enum OrderType
    {
        LIMIT,
        MARKET,
        STOP_LOSS,
        STOP_LOSS_LIMIT
    }

    public enum TimeInForce
    {
        GTC,  // Good Till Canceled
        IOC,  // Immediate or Cancel
        FOK   // Fill or Kill
    }

    /// <summary>
    /// Stress test implementation for the trading system
    /// </summary>
    public class StressTest
    {
        private readonly CommonLib.Api.IdentityService _identityService;
        private readonly CommonLib.Api.TradingService _tradingService;
        private readonly CommonLib.Api.MarketDataService _marketDataService;
        private readonly TestLogger _logger;
        private readonly ReportGenerator _reportGenerator;
        private readonly List<OperationResult> _results = new();
        private readonly List<UserInfo> _users = new();
        private string _testDirectory;
        private string[] _testSymbols = { "BTC-USDT", "ETH-USDT", "XRP-USDT", "ADA-USDT", "SOL-USDT" };

        public StressTest(
            CommonLib.Api.IdentityService identityService,
            CommonLib.Api.TradingService tradingService,
            CommonLib.Api.MarketDataService marketDataService,
            TestLogger logger,
            ReportGenerator reportGenerator)
        {
            _identityService = identityService;
            _tradingService = tradingService;
            _marketDataService = marketDataService;
            _logger = logger;
            _reportGenerator = reportGenerator;
        }

        /// <summary>
        /// Run the stress test with specified number of users and orders per user
        /// </summary>
        public async Task RunAsync(int userCount, int ordersPerUser)
        {
            // Create log directory
            _testDirectory = _logger.CreateLogDirectory("Stress");

            // Initialize test
            _logger.Info($"Starting stress test with {userCount} users and {ordersPerUser} orders per user");

            // Total operations = user registrations + orders
            int totalOperations = userCount + (userCount * ordersPerUser);
            var statusBar = new StatusBar(totalOperations);
            statusBar.Start();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Get available symbols from market data service
                _logger.Info("Fetching available trading symbols...");
                var symbols = await GetAvailableSymbolsAsync();
                if (symbols.Length > 0)
                {
                    _testSymbols = symbols;
                }

                // Step 1: Register users
                await RegisterUsersAsync(userCount, statusBar);

                // Step 2: Create orders for each user
                await CreateOrdersAsync(ordersPerUser, statusBar);

                // Finish test
                stopwatch.Stop();
                var statistics = statusBar.Stop();
                statistics.Results = _results;

                // Display stress test summary with ordering metrics being the primary focus
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule("[yellow]Stress Test Summary[/]").RuleStyle("grey").LeftJustified());
                AnsiConsole.MarkupLine($"[blue]Users Created:[/] {_users.Count}");
                AnsiConsole.MarkupLine($"[blue]Orders Per User:[/] {ordersPerUser}");
                AnsiConsole.MarkupLine($"[blue]Total Operations:[/] {statistics.TotalOperations}");
                AnsiConsole.MarkupLine($"[blue]Elapsed Time:[/] {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");
                AnsiConsole.WriteLine();

                // Order-specific metrics section
                AnsiConsole.Write(new Rule("[yellow]Order Creation Metrics[/]").RuleStyle("grey").LeftJustified());
                AnsiConsole.MarkupLine($"[green]Successful Orders:[/] {statistics.OrderSuccessCount}");
                AnsiConsole.MarkupLine($"[red]Failed Orders:[/] {statistics.OrderFailureCount}");
                AnsiConsole.MarkupLine($"[blue]Order Success Rate:[/] {statistics.OrderSuccessRate:F2}%");
                AnsiConsole.MarkupLine($"[blue]Orders Per Second:[/] {statistics.OrderRequestsPerSecond:F2}");
                AnsiConsole.MarkupLine($"[blue]Average Order Latency:[/] {statistics.OrderAverageLatencyMs:F2}ms");
                AnsiConsole.WriteLine();

                // Detailed order latency metrics
                AnsiConsole.Write(new Rule("[yellow]Order Latency Percentiles[/]").RuleStyle("grey").LeftJustified());
                AnsiConsole.MarkupLine($"[blue]Min Latency:[/] {statistics.OrderMinLatencyMs}ms");
                AnsiConsole.MarkupLine($"[blue]P50 Latency:[/] {statistics.OrderP50LatencyMs}ms");
                AnsiConsole.MarkupLine($"[blue]P90 Latency:[/] {statistics.OrderP90LatencyMs}ms");
                AnsiConsole.MarkupLine($"[blue]P95 Latency:[/] {statistics.OrderP95LatencyMs}ms");
                AnsiConsole.MarkupLine($"[blue]P99 Latency:[/] {statistics.OrderP99LatencyMs}ms");
                AnsiConsole.MarkupLine($"[blue]Max Latency:[/] {statistics.OrderMaxLatencyMs}ms");
                AnsiConsole.WriteLine();

                _logger.Success($"Stress test completed in {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");

                // Generate report
                _reportGenerator.GenerateStressTestReport(_testDirectory, statistics, userCount, ordersPerUser);
            }
            catch (Exception ex)
            {
                _logger.Error($"Stress test failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Inner exception: {ex.InnerException.Message}");
                }
                _logger.Error(ex.StackTrace ?? "No stack trace available");
            }
        }

        /// <summary>
        /// Register the specified number of test users
        /// </summary>
        private async Task RegisterUsersAsync(int userCount, StatusBar statusBar)
        {
            _logger.Info($"Registering {userCount} test users...");

            for (int i = 0; i < userCount; i++)
            {
                string username = $"stresstest_user_{DateTime.Now:yyyyMMddHHmmss}_{i}";
                string email = $"{username}@example.com";
                string password = "Test@123";

                var registerRequest = new RegisterRequest
                {
                    Username = username,
                    Email = email,
                    Password = password
                };

                string operationType = $"Register User {i + 1}/{userCount}";
                _logger.Debug($"Registering user: {username}");

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var result = await _identityService.RegisterAsync(registerRequest);
                    stopwatch.Stop();

                    _users.Add(new UserInfo
                    {
                        UserId = result.UserId,
                        Email = email,
                        Username = username,
                        Token = result.Token
                    });

                    _results.Add(new OperationResult
                    {
                        OperationType = operationType,
                        UserId = result.UserId,
                        Success = true,
                        LatencyMs = stopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.Now
                    });

                    statusBar.ReportSuccess(stopwatch.ElapsedMilliseconds);
                    _logger.Debug($"Registered user {username} with ID {result.UserId}");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _results.Add(new OperationResult
                    {
                        OperationType = operationType,
                        Success = false,
                        LatencyMs = stopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.Now,
                        ErrorMessage = ex.Message
                    });

                    statusBar.ReportFailure();
                    _logger.Warning($"Failed to register user {username}: {ex.Message}");
                }
            }

            _logger.Info($"Registered {_users.Count} users successfully");
        }

        /// <summary>
        /// Create the specified number of orders for each registered user
        /// </summary>
        private async Task CreateOrdersAsync(int ordersPerUser, StatusBar statusBar)
        {
            _logger.Info($"Creating {ordersPerUser} orders for each user...");

            var random = new Random();
            var userTasks = new List<Task>();

            // 为每个用户创建一个专门的任务
            foreach (var user in _users)
            {
                // 为每个用户创建一个独立的任务，在其中同步创建订单
                var userTask = Task.Run(async () =>
                {
                    _logger.Info($"Starting order creation for user {user.Username}");

                    // 创建一个独立的随机数生成器，避免线程安全问题
                    var userRandom = new Random(Guid.NewGuid().GetHashCode());

                    // 同步执行该用户的所有订单创建
                    for (int i = 0; i < ordersPerUser; i++)
                    {
                        var orderSide = userRandom.Next(2) == 0 ? OrderSide.BUY : OrderSide.SELL;
                        var symbol = _testSymbols[userRandom.Next(_testSymbols.Length)];
                        var price = Math.Round(userRandom.NextDouble() * 1000, 2) + 100; // Random price between 100-1100
                        var quantity = Math.Round(userRandom.NextDouble() * 10, 4) + 0.1; // Random quantity between 0.1-10.1

                        var orderRequest = new CreateOrderRequest
                        {
                            Symbol = symbol,
                            Side = orderSide.ToString(),
                            Type = OrderType.LIMIT.ToString(),
                            Price = (decimal)price,
                            Quantity = (decimal)quantity,
                            TimeInForce = TimeInForce.GTC.ToString()
                        };

                        string operationType = $"Create {orderSide} Order for {symbol}";
                        _logger.Debug($"Creating order {i + 1}/{ordersPerUser} for user {user.Username}: {orderSide} {quantity} {symbol} @ {price}");

                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            var result = await _tradingService.CreateOrderAsync(user.Token, orderRequest);
                            stopwatch.Stop();

                            lock (_results)
                            {
                                _results.Add(new OperationResult
                                {
                                    OperationType = operationType,
                                    UserId = user.UserId,
                                    Success = true,
                                    LatencyMs = stopwatch.ElapsedMilliseconds,
                                    Timestamp = DateTime.Now
                                });
                            }

                            statusBar.ReportSuccess(stopwatch.ElapsedMilliseconds, true); // Mark as order creation
                            _logger.Debug($"Created order for user {user.Username}: {result.OrderId}");
                        }
                        catch (Exception ex)
                        {
                            stopwatch.Stop();

                            lock (_results)
                            {
                                _results.Add(new OperationResult
                                {
                                    OperationType = operationType,
                                    UserId = user.UserId,
                                    Success = false,
                                    LatencyMs = stopwatch.ElapsedMilliseconds,
                                    Timestamp = DateTime.Now,
                                    ErrorMessage = ex.Message
                                });
                            }

                            statusBar.ReportFailure(true); // Mark as order creation
                            _logger.Debug($"Failed to create order for user {user.Username}: {ex.Message}");
                        }

                        // 加入小的随机延迟，避免完全同时发送请求
                        await Task.Delay(userRandom.Next(5, 20));
                    }

                    _logger.Info($"Completed creating {ordersPerUser} orders for user {user.Username}");
                });

                userTasks.Add(userTask);
            }

            // 等待所有用户任务完成
            await Task.WhenAll(userTasks);

            _logger.Info("Completed order creation for all users");
        }

        /// <summary>
        /// Get available trading symbols from the market data service
        /// </summary>
        private async Task<string[]> GetAvailableSymbolsAsync()
        {
            try
            {
                var symbolsResponse = await _marketDataService.GetSymbolsAsync();
                if (symbolsResponse.Symbols != null && symbolsResponse.Symbols.Count > 0)
                {
                    return symbolsResponse.Symbols
                        .Where(s => s.IsActive)
                        .Select(s => s.Symbol)
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to retrieve symbols: {ex.Message}. Using default test symbols.");
            }

            return _testSymbols;
        }

        /// <summary>
        /// User information container for testing
        /// </summary>
        private class UserInfo
        {
            public string UserId { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Token { get; set; }
        }
    }
}