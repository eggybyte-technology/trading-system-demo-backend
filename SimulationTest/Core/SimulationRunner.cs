using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using SimulationTest.Helpers;
using CommonLib.Models.Identity;

namespace SimulationTest.Core
{
    /// <summary>
    /// Main class for running the trading system simulation
    /// </summary>
    public class SimulationRunner
    {
        private HttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        private List<UserCredential> _users = new List<UserCredential>();
        private OrderService? _orderService;
        private List<string> _symbolList = new List<string>();
        private readonly Dictionary<string, (decimal Min, decimal Max)> _priceRanges = new();
        private readonly string _logFile;
        private readonly object _simLogLock = new object();

        private string _tradingHost = string.Empty;
        private string _identityHost = string.Empty;
        private string _marketDataHost = string.Empty;
        private string _accountHost = string.Empty;
        private string _riskHost = string.Empty;
        private string _notificationHost = string.Empty;
        private int _numUsers;
        private int _numOrders;
        private int _concurrency;
        private int _timeout;
        private bool _verbose;
        private bool _consoleOutput = true; // Default to true for console output
        private string _simulationMode = "random";
        private int _burstSize = 10;
        private int _baseDelay = 0;
        private (decimal Min, decimal Max) _quantityRange;
        private (int Min, int Max) _requestDelay;
        private int _totalUsers;
        private int _ordersPerUser;

        /// <summary>
        /// Initializes a new instance of the SimulationRunner class
        /// </summary>
        /// <param name="configuration">Configuration for the simulation</param>
        public SimulationRunner(IConfiguration configuration)
        {
            _configuration = configuration;

            // Set up log file
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");
            _logFile = Path.Combine("logs", $"simulation_log_{timestamp}.txt");

            // Initialize the log file
            using var logWriter = new StreamWriter(_logFile, false);
            logWriter.WriteLine("=== Trading System Simulation Log ===");
            logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine("=================================\n");

            // Set default values from configuration
            _tradingHost = configuration["Services:TradingHost"] ?? "http://trading.trading-system.local";
            _identityHost = configuration["Services:IdentityHost"] ?? "http://identity.trading-system.local";
            _marketDataHost = configuration["Services:MarketDataHost"] ?? "http://market-data.trading-system.local";
            _accountHost = configuration["Services:AccountHost"] ?? "http://account.trading-system.local";
            _riskHost = configuration["Services:RiskHost"] ?? "http://risk.trading-system.local";
            _notificationHost = configuration["Services:NotificationHost"] ?? "http://notification.trading-system.local";
            _numUsers = int.Parse(configuration["Simulation:Users"] ?? "10");
            _numOrders = int.Parse(configuration["Simulation:OrdersPerUser"] ?? "100");
            _concurrency = int.Parse(configuration["Simulation:Concurrency"] ?? "10");
            _timeout = int.Parse(configuration["Simulation:TimeoutSeconds"] ?? "30");
            _verbose = bool.Parse(configuration["Simulation:Verbose"] ?? "false");
            _consoleOutput = bool.Parse(configuration["Simulation:ConsoleOutput"] ?? "true");
            _simulationMode = configuration["Simulation:Mode"] ?? "random";
            _burstSize = int.Parse(configuration["Simulation:BurstSize"] ?? "10");
            _baseDelay = int.Parse(configuration["Simulation:BaseDelayMs"] ?? "0");

            // Default price ranges
            _priceRanges["BTC-USDT"] = (1000m, 100000m);
            _priceRanges["ETH-USDT"] = (100m, 10000m);
            _priceRanges["BNB-USDT"] = (10m, 1000m);

            // Load symbols from configuration
            var symbolsConfig = _configuration.GetSection("Simulation:Symbols").Get<string[]>();
            if (symbolsConfig != null && symbolsConfig.Length > 0)
            {
                _symbolList = symbolsConfig.ToList();
            }
            else
            {
                _symbolList.AddRange(new[] { "BTC-USDT", "ETH-USDT", "BNB-USDT" });
            }

            // Default quantity range
            _quantityRange = (0.01m, 1.0m);

            // Default request delay range
            _requestDelay = (0, 100);

            // Initialize HTTP client factory
            _httpClientFactory = new HttpClientFactory();
            _httpClientFactory.Configure(_timeout);

            // Initialize service URLs
            var serviceUrls = new Dictionary<string, string>
            {
                { "identity", _identityHost },
                { "trading", _tradingHost },
                { "market-data", _marketDataHost },
                { "account", _accountHost },
                { "risk", _riskHost },
                { "notification", _notificationHost }
            };
            _httpClientFactory.ConfigureServiceUrls(serviceUrls);
        }

        /// <summary>
        /// Configure the simulation with command line arguments
        /// </summary>
        /// <param name="tradingHost">Trading service host URL</param>
        /// <param name="identityHost">Identity service host URL</param>
        /// <param name="marketDataHost">Market data service host URL</param>
        /// <param name="accountHost">Account service host URL</param>
        /// <param name="riskHost">Risk service host URL</param>
        /// <param name="notificationHost">Notification service host URL</param>
        /// <param name="numUsers">Number of users to simulate</param>
        /// <param name="numOrders">Number of orders per user</param>
        /// <param name="concurrency">Number of concurrent threads</param>
        /// <param name="timeout">HTTP request timeout in seconds</param>
        /// <param name="verbose">Whether to log verbose messages</param>
        /// <param name="simulationMode">Simulation mode</param>
        /// <param name="burstSize">Number of orders in a burst</param>
        /// <param name="baseDelay">Base delay between requests</param>
        /// <param name="symbols">List of symbols to use</param>
        /// <param name="consoleOutput">Whether to output logs to console (will still log to files)</param>
        public void Configure(
            string? tradingHost = null,
            string? identityHost = null,
            string? marketDataHost = null,
            string? accountHost = null,
            string? riskHost = null,
            string? notificationHost = null,
            int? numUsers = null,
            int? numOrders = null,
            int? concurrency = null,
            int? timeout = null,
            bool? verbose = null,
            string? simulationMode = null,
            int? burstSize = null,
            int? baseDelay = null,
            List<string>? symbols = null,
            bool? consoleOutput = null)
        {
            // Log all configuration options that are being set
            Console.WriteLine("Configuring SimulationRunner with the following options:");

            // For each parameter, show what's changing (or not)
            Console.WriteLine($"  tradingHost: {_tradingHost} -> {tradingHost ?? "unchanged"}");
            Console.WriteLine($"  identityHost: {_identityHost} -> {identityHost ?? "unchanged"}");
            Console.WriteLine($"  marketDataHost: {_marketDataHost} -> {marketDataHost ?? "unchanged"}");
            Console.WriteLine($"  accountHost: {_accountHost} -> {accountHost ?? "unchanged"}");
            Console.WriteLine($"  riskHost: {_riskHost} -> {riskHost ?? "unchanged"}");
            Console.WriteLine($"  notificationHost: {_notificationHost} -> {notificationHost ?? "unchanged"}");
            Console.WriteLine($"  numUsers: {_numUsers} -> {numUsers?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  numOrders: {_numOrders} -> {numOrders?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  concurrency: {_concurrency} -> {concurrency?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  timeout: {_timeout} -> {timeout?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  verbose: {_verbose} -> {verbose?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  consoleOutput: {_consoleOutput} -> {consoleOutput?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  simulationMode: {_simulationMode} -> {simulationMode ?? "unchanged"}");
            Console.WriteLine($"  burstSize: {_burstSize} -> {burstSize?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  baseDelay: {_baseDelay} -> {baseDelay?.ToString() ?? "unchanged"}");
            Console.WriteLine($"  symbols: [{string.Join(", ", _symbolList)}] -> {(symbols != null ? $"[{string.Join(", ", symbols)}]" : "unchanged")}");

            // Also log to the simulation log file
            using (var logWriter = new StreamWriter(_logFile, true))
            {
                logWriter.WriteLine("Configuration changes:");
                logWriter.WriteLine($"  tradingHost: {_tradingHost} -> {tradingHost ?? "unchanged"}");
                logWriter.WriteLine($"  identityHost: {_identityHost} -> {identityHost ?? "unchanged"}");
                logWriter.WriteLine($"  marketDataHost: {_marketDataHost} -> {marketDataHost ?? "unchanged"}");
                logWriter.WriteLine($"  accountHost: {_accountHost} -> {accountHost ?? "unchanged"}");
                logWriter.WriteLine($"  riskHost: {_riskHost} -> {riskHost ?? "unchanged"}");
                logWriter.WriteLine($"  notificationHost: {_notificationHost} -> {notificationHost ?? "unchanged"}");
                logWriter.WriteLine($"  numUsers: {_numUsers} -> {numUsers?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  numOrders: {_numOrders} -> {numOrders?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  concurrency: {_concurrency} -> {concurrency?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  timeout: {_timeout} -> {timeout?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  verbose: {_verbose} -> {verbose?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  consoleOutput: {_consoleOutput} -> {consoleOutput?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  simulationMode: {_simulationMode} -> {simulationMode ?? "unchanged"}");
                logWriter.WriteLine($"  burstSize: {_burstSize} -> {burstSize?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  baseDelay: {_baseDelay} -> {baseDelay?.ToString() ?? "unchanged"}");
                logWriter.WriteLine($"  symbols: [{string.Join(", ", _symbolList)}] -> {(symbols != null ? $"[{string.Join(", ", symbols)}]" : "unchanged")}");
                logWriter.WriteLine();
            }

            // Override config with command line parameters if provided
            _tradingHost = tradingHost ?? _tradingHost;
            _identityHost = identityHost ?? _identityHost;
            _marketDataHost = marketDataHost ?? _marketDataHost;
            _accountHost = accountHost ?? _accountHost;
            _riskHost = riskHost ?? _riskHost;
            _notificationHost = notificationHost ?? _notificationHost;

            _numUsers = numUsers ?? _numUsers;
            _numOrders = numOrders ?? _numOrders;
            _concurrency = concurrency ?? _concurrency;
            _timeout = timeout ?? _timeout;
            _verbose = verbose ?? _verbose;
            _consoleOutput = consoleOutput ?? _consoleOutput;
            _simulationMode = simulationMode?.ToLower() ?? _simulationMode;
            _burstSize = burstSize ?? _burstSize;
            _baseDelay = baseDelay ?? _baseDelay;

            // Initialize HTTP clients
            Console.WriteLine("Initializing HTTP clients with updated configuration...");
            _httpClientFactory.InitializeHttpClients(
                _tradingHost,
                _identityHost,
                _marketDataHost,
                _accountHost,
                _riskHost,
                _notificationHost,
                _timeout);

            // Override symbols if provided
            if (symbols != null && symbols.Count > 0)
            {
                Console.WriteLine($"Updating symbol list from: [{string.Join(", ", _symbolList)}] to [{string.Join(", ", symbols)}]");
                _symbolList = symbols;
            }

            // Load price ranges from configuration
            var priceRangeConfig = _configuration.GetSection("OrderGeneration:PriceRange");
            foreach (var symbol in _symbolList)
            {
                var symbolSection = priceRangeConfig.GetSection(symbol);
                if (symbolSection.Exists())
                {
                    var minPrice = decimal.Parse(symbolSection["Min"] ?? "90.00");
                    var maxPrice = decimal.Parse(symbolSection["Max"] ?? "110.00");
                    _priceRanges[symbol] = (minPrice, maxPrice);
                }
                else if (!_priceRanges.ContainsKey(symbol))
                {
                    // Set default if not already defined and not in config
                    _priceRanges[symbol] = (90.00m, 110.00m);
                }
            }

            // Load quantity range
            var qtyConfig = _configuration.GetSection("OrderGeneration:QuantityRange");
            _quantityRange = (
                decimal.Parse(qtyConfig["Min"] ?? "0.01"),
                decimal.Parse(qtyConfig["Max"] ?? "1.00")
            );

            // Load request delay
            var delayConfig = _configuration.GetSection("OrderGeneration:RequestDelay");
            _requestDelay = (
                int.Parse(delayConfig["Min"] ?? "0"),
                int.Parse(delayConfig["Max"] ?? "50")
            );

            // Log the final configuration
            Console.WriteLine("\nFinal configuration:");
            Console.WriteLine($"  tradingHost: {_tradingHost}");
            Console.WriteLine($"  identityHost: {_identityHost}");
            Console.WriteLine($"  marketDataHost: {_marketDataHost}");
            Console.WriteLine($"  accountHost: {_accountHost}");
            Console.WriteLine($"  riskHost: {_riskHost}");
            Console.WriteLine($"  notificationHost: {_notificationHost}");
            Console.WriteLine($"  numUsers: {_numUsers}");
            Console.WriteLine($"  numOrders: {_numOrders}");
            Console.WriteLine($"  concurrency: {_concurrency}");
            Console.WriteLine($"  timeout: {_timeout}");
            Console.WriteLine($"  verbose: {_verbose}");
            Console.WriteLine($"  consoleOutput: {_consoleOutput}");
            Console.WriteLine($"  simulationMode: {_simulationMode}");
            Console.WriteLine($"  burstSize: {_burstSize}");
            Console.WriteLine($"  baseDelay: {_baseDelay}");
            Console.WriteLine($"  symbols: [{string.Join(", ", _symbolList)}]");
        }

        /// <summary>
        /// Run the simulation
        /// </summary>
        /// <returns>Exit code (0 for success, non-zero for failure)</returns>
        public async Task<int> RunAsync()
        {
            Console.WriteLine("Starting SimulationRunner execution...");
            _totalStopwatch.Start();

            try
            {
                Console.WriteLine("Checking if services are available...");
                using (var logWriter = new StreamWriter(_logFile, true))
                {
                    logWriter.WriteLine("=== Starting Simulation ===");
                    logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    logWriter.WriteLine($"Simulation mode: {_simulationMode}");
                    logWriter.WriteLine();

                    var servicesAvailable = await CheckServicesAvailabilityAsync(_verbose, logWriter);
                    if (!servicesAvailable)
                    {
                        Console.WriteLine("Aborting simulation due to service availability issues.");
                        logWriter.WriteLine("Simulation aborted due to service availability issues.");
                        return 1;
                    }
                }

                // Log the start of the simulation
                var simulationStartTime = DateTime.Now;
                var simulationLogFile = Path.Combine("logs", $"simulation_log_{simulationStartTime:yyyyMMdd_HHmmss}.txt");
                using var simulationLog = new StreamWriter(simulationLogFile, true);
                simulationLog.WriteLine("=== Trading System Simulation Log ===");
                simulationLog.WriteLine($"Start Time: {simulationStartTime:yyyy-MM-dd HH:mm:ss}");

                // Only display in console if console output is enabled
                if (_consoleOutput)
                {
                    // Display header
                    AnsiConsole.Write(
                        new FigletText("Trading System")
                            .LeftJustified()
                            .Color(Color.Green));

                    AnsiConsole.WriteLine("Simulation Test Tool");
                    AnsiConsole.WriteLine("--------------------\n");

                    // Display configuration
                    AnsiConsole.MarkupLine($"[green]Trading Host:[/] {_tradingHost}");
                    AnsiConsole.MarkupLine($"[green]Identity Host:[/] {_identityHost}");
                    AnsiConsole.MarkupLine($"[green]Market Data Host:[/] {_marketDataHost}");
                    AnsiConsole.MarkupLine($"[green]Account Host:[/] {_accountHost}");
                    AnsiConsole.MarkupLine($"[green]Risk Host:[/] {_riskHost}");
                    AnsiConsole.MarkupLine($"[green]Notification Host:[/] {_notificationHost}");
                    AnsiConsole.MarkupLine($"[green]Simulated Users:[/] {_numUsers}");
                    AnsiConsole.MarkupLine($"[green]Orders per User:[/] {_numOrders}");
                    AnsiConsole.MarkupLine($"[green]Concurrency:[/] {_concurrency}");
                    AnsiConsole.MarkupLine($"[green]Total Orders to Submit:[/] {_numUsers * _numOrders}");
                    AnsiConsole.MarkupLine($"[green]Symbols:[/] {string.Join(", ", _symbolList)}");
                    AnsiConsole.MarkupLine($"[green]Simulation Mode:[/] {_simulationMode}");
                    AnsiConsole.MarkupLine($"[green]Console Output:[/] {(_consoleOutput ? "Enabled" : "Disabled")}");
                }

                // Log configuration to file
                simulationLog.WriteLine("\n=== Configuration ===");
                simulationLog.WriteLine($"Trading Host: {_tradingHost}");
                simulationLog.WriteLine($"Identity Host: {_identityHost}");
                simulationLog.WriteLine($"Market Data Host: {_marketDataHost}");
                simulationLog.WriteLine($"Account Host: {_accountHost}");
                simulationLog.WriteLine($"Risk Host: {_riskHost}");
                simulationLog.WriteLine($"Notification Host: {_notificationHost}");
                simulationLog.WriteLine($"Simulated Users: {_numUsers}");
                simulationLog.WriteLine($"Orders per User: {_numOrders}");
                simulationLog.WriteLine($"Concurrency: {_concurrency}");
                simulationLog.WriteLine($"Simulation Mode: {_simulationMode}");
                simulationLog.WriteLine($"Console Output: {(_consoleOutput ? "Enabled" : "Disabled")}");

                // Create users
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                // Fix: Pass the HttpClientFactory to UserService along with the identity client
                var userService = new UserService(
                    _httpClientFactory.GetClient("identity"),
                    _httpClientFactory,
                    jsonOptions);

                _users.AddRange(await userService.CreateTestUsersAsync(_numUsers, _verbose));

                // Log user creation results
                simulationLog.WriteLine($"\n=== Created {_users.Count} test users ===");

                if (_users.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]Failed to create any test users. Aborting simulation.[/]");
                    simulationLog.WriteLine("Simulation aborted: No users were created successfully.");
                    return 1;
                }

                if (_users.Count < _numUsers)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Only {_users.Count} out of {_numUsers} users were created successfully.[/]");
                    simulationLog.WriteLine($"Warning: Only {_users.Count} out of {_numUsers} users were created successfully.");
                }

                // Run the simulation
                AnsiConsole.MarkupLine("\n[blue]Starting simulation test...[/]");

                _totalStopwatch.Start();
                simulationLog.WriteLine("\n=== Simulation Started ===");
                simulationLog.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                simulationLog.WriteLine($"Mode: {_simulationMode}");
                simulationLog.WriteLine($"Users: {_users.Count}");
                simulationLog.WriteLine($"Orders per User: {_numOrders}");
                simulationLog.WriteLine($"Total Expected Orders: {_users.Count * _numOrders}");

                // Fix: Pass HttpClientFactory instead of HttpClient to OrderService
                var orderService = new OrderService(
                    _httpClientFactory,
                    _users.Count,
                    _numOrders,
                    _symbolList,
                    _priceRanges,
                    _baseDelay,
                    _requestDelay);

                // Create a progress display with additional stats
                if (_consoleOutput)
                {
                    AnsiConsole.Progress()
                        .AutoClear(false)
                        .Columns(new ProgressColumn[] {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new SpinnerColumn(),
                        })
                        .Start(ctx =>
                        {
                            var overallTask = ctx.AddTask("[green]Overall Progress[/]", maxValue: _numUsers * _numOrders);
                            var errorRateTask = ctx.AddTask("[red]Error Rate[/]", maxValue: 100);
                            var successCountTask = ctx.AddTask("[blue]Successful Operations[/]", maxValue: _numUsers * _numOrders);

                            // Create a separate task to monitor progress
                            var progressMonitorCts = new CancellationTokenSource();
                            var progressMonitorTask = Task.Run(async () =>
                            {
                                int previousSuccessCount = 0;
                                int previousFailureCount = 0;

                                // Update stats every second
                                while (!progressMonitorCts.Token.IsCancellationRequested)
                                {
                                    await Task.Delay(1000, progressMonitorCts.Token);

                                    if (progressMonitorCts.Token.IsCancellationRequested)
                                        break;

                                    // Get current counts
                                    int currentSuccessCount = orderService.SuccessCount;
                                    int currentFailureCount = orderService.FailureCount;
                                    int totalOperations = currentSuccessCount + currentFailureCount;

                                    // Calculate new progress values
                                    int newSuccesses = currentSuccessCount - previousSuccessCount;
                                    int newFailures = currentFailureCount - previousFailureCount;

                                    // Update overall progress
                                    overallTask.Increment(newSuccesses + newFailures);

                                    // Update success count display
                                    successCountTask.Value = currentSuccessCount;

                                    // Calculate and update error rate
                                    double errorRate = totalOperations > 0
                                        ? (double)currentFailureCount / totalOperations * 100
                                        : 0;
                                    errorRateTask.Value = errorRate;

                                    // If we're done, break the stats update loop
                                    if (totalOperations >= _numUsers * _numOrders)
                                        break;

                                    // Store current counts for next iteration
                                    previousSuccessCount = currentSuccessCount;
                                    previousFailureCount = currentFailureCount;
                                }
                            }, progressMonitorCts.Token);

                            try
                            {
                                // Process all users in parallel
                                Parallel.ForEach(_users, new ParallelOptions { MaxDegreeOfParallelism = _concurrency }, user =>
                                {
                                    try
                                    {
                                        // Submit orders based on simulation mode
                                        switch (_simulationMode)
                                        {
                                            case "market":
                                                orderService.SubmitMarketOrdersAsync(user, _numOrders, null, _verbose, _consoleOutput).GetAwaiter().GetResult();
                                                break;
                                            // Implement other strategies as needed
                                            case "random":
                                            default:
                                                orderService.SubmitRandomOrdersAsync(user, _numOrders, null, _verbose, _consoleOutput).GetAwaiter().GetResult();
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Log any exceptions that occur during order processing
                                        lock (_simLogLock)
                                        {
                                            using var writer = new StreamWriter(_logFile, true);
                                            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error processing orders for user {user.Email}: {ex.Message}");
                                            if (_consoleOutput)
                                            {
                                                AnsiConsole.MarkupLine($"[red]Error processing orders for user {user.Email}: {ex.Message}[/]");
                                            }
                                        }
                                    }
                                });
                            }
                            finally
                            {
                                // Stop the monitoring task
                                progressMonitorCts.Cancel();
                                try
                                {
                                    progressMonitorTask.Wait(1000); // Wait for the task to finish with timeout
                                }
                                catch
                                {
                                    // Ignore any task cancellation exceptions
                                }
                                progressMonitorCts.Dispose();
                            }
                        });
                }
                else
                {
                    // Non-console mode - run without visuals but collect stats
                    Parallel.ForEach(_users, new ParallelOptions { MaxDegreeOfParallelism = _concurrency }, user =>
                    {
                        try
                        {
                            // Submit orders based on simulation mode
                            switch (_simulationMode)
                            {
                                case "market":
                                    orderService.SubmitMarketOrdersAsync(user, _numOrders, null, _verbose, _consoleOutput).GetAwaiter().GetResult();
                                    break;
                                // Implement other strategies as needed
                                case "random":
                                default:
                                    orderService.SubmitRandomOrdersAsync(user, _numOrders, null, _verbose, _consoleOutput).GetAwaiter().GetResult();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log any exceptions that occur during order processing
                            lock (_simLogLock)
                            {
                                using var writer = new StreamWriter(_logFile, true);
                                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error processing orders for user {user.Email}: {ex.Message}");
                            }
                        }
                    });
                }

                _totalStopwatch.Stop();

                simulationLog.WriteLine("\n=== Simulation Completed ===");
                simulationLog.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                simulationLog.WriteLine($"Duration: {_totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
                simulationLog.WriteLine($"Total Operations: {orderService.TotalOperations}");
                simulationLog.WriteLine($"Successful Operations: {orderService.SuccessCount}");
                simulationLog.WriteLine($"Failed Operations: {orderService.FailureCount}");

                // Display and save results
                DisplayResults(orderService.SuccessCount, orderService.FailureCount, orderService.Latencies, _totalStopwatch.Elapsed, _users);

                // Final log entry
                simulationLog.WriteLine("\n=== Simulation Log Complete ===");
                AnsiConsole.MarkupLine($"\n[green]Simulation complete! Full log saved to [blue]{simulationLogFile}[/][/]");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation failed with error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                using (var logWriter = new StreamWriter(_logFile, true))
                {
                    logWriter.WriteLine("=== Simulation Failed ===");
                    logWriter.WriteLine($"Error: {ex.Message}");
                    logWriter.WriteLine(ex.StackTrace);
                }

                return 1;
            }
        }

        /// <summary>
        /// Display simulation results and save to file
        /// </summary>
        private void DisplayResults(int successCount, int failureCount, IEnumerable<long> latencies, TimeSpan elapsed, List<UserCredential> users)
        {
            // Calculate statistics
            double successRate = successCount > 0
                ? (double)successCount / (successCount + failureCount) * 100
                : 0;

            var elapsedSeconds = elapsed.TotalSeconds;
            double ordersPerSecond = (successCount + failureCount) / elapsedSeconds;

            var latenciesArray = latencies.ToArray();
            Array.Sort(latenciesArray);

            var minLatency = latenciesArray.Length > 0 ? latenciesArray[0] : 0;
            var maxLatency = latenciesArray.Length > 0 ? latenciesArray[^1] : 0;
            var avgLatency = latenciesArray.Length > 0 ? latenciesArray.Average() : 0;

            var p50Index = (int)(latenciesArray.Length * 0.5);
            var p95Index = (int)(latenciesArray.Length * 0.95);
            var p99Index = (int)(latenciesArray.Length * 0.99);

            var p50Latency = latenciesArray.Length > 0 ? latenciesArray[p50Index] : 0;
            var p95Latency = latenciesArray.Length > 0 ? latenciesArray[p95Index] : 0;
            var p99Latency = latenciesArray.Length > 0 ? latenciesArray[p99Index] : 0;

            // Display results in a table
            var table = new Table()
                .BorderColor(Color.Green)
                .Title("Simulation Results")
                .AddColumn("Metric")
                .AddColumn("Value");

            table.AddRow("Total Orders", $"{successCount + failureCount}");
            table.AddRow("Successful Orders", $"{successCount}");
            table.AddRow("Failed Orders", $"{failureCount}");
            table.AddRow("Success Rate", $"{successRate:F2}%");
            table.AddRow("Total Time", $"{elapsedSeconds:F2} seconds");
            table.AddRow("Orders Per Second", $"{ordersPerSecond:F2}");
            table.AddRow("Min Latency", $"{minLatency} ms");
            table.AddRow("Max Latency", $"{maxLatency} ms");
            table.AddRow("Average Latency", $"{avgLatency:F2} ms");
            table.AddRow("Median Latency (P50)", $"{p50Latency} ms");
            table.AddRow("P95 Latency", $"{p95Latency} ms");
            table.AddRow("P99 Latency", $"{p99Latency} ms");

            AnsiConsole.Write(table);

            // Save results to a file
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = Path.Combine("logs", $"simulation_results_{timestamp}.txt");

            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    writer.WriteLine("Trading System Simulation Results");
                    writer.WriteLine("=============================");
                    writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Trading Host: {_tradingHost}");
                    writer.WriteLine($"Identity Host: {_identityHost}");
                    writer.WriteLine($"Market Data Host: {_marketDataHost}");
                    writer.WriteLine($"Account Host: {_accountHost}");
                    writer.WriteLine($"Risk Host: {_riskHost}");
                    writer.WriteLine($"Notification Host: {_notificationHost}");
                    writer.WriteLine($"Simulation Mode: {_simulationMode}");
                    writer.WriteLine();
                    writer.WriteLine("Performance Metrics:");
                    writer.WriteLine($"Total Orders: {successCount + failureCount}");
                    writer.WriteLine($"Successful Orders: {successCount}");
                    writer.WriteLine($"Failed Orders: {failureCount}");
                    writer.WriteLine($"Success Rate: {successRate:F2}%");
                    writer.WriteLine($"Total Time: {elapsedSeconds:F2} seconds");
                    writer.WriteLine($"Orders Per Second: {ordersPerSecond:F2}");
                    writer.WriteLine();
                    writer.WriteLine("Latency Metrics:");
                    writer.WriteLine($"Min Latency: {minLatency} ms");
                    writer.WriteLine($"Max Latency: {maxLatency} ms");
                    writer.WriteLine($"Average Latency: {avgLatency:F2} ms");
                    writer.WriteLine($"Median Latency (P50): {p50Latency} ms");
                    writer.WriteLine($"P95 Latency: {p95Latency} ms");
                    writer.WriteLine($"P99 Latency: {p99Latency} ms");

                    // Add detailed summary of users
                    writer.WriteLine();
                    writer.WriteLine("User Summary:");
                    foreach (var user in users)
                    {
                        writer.WriteLine($"User: {user.Email}, User ID: {user.UserId}");
                    }
                }

                AnsiConsole.MarkupLine($"[green]Results saved to [blue]{filename}[/][/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error saving results: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Check availability of all services
        /// </summary>
        private async Task<bool> CheckServicesAvailabilityAsync(bool verbose, StreamWriter log)
        {
            var services = new Dictionary<string, string>
            {
                { "identity", $"{_identityHost}/health" },
                { "trading", $"{_tradingHost}/health" },
                { "market-data", $"{_marketDataHost}/health" },
                { "account", $"{_accountHost}/health" },
                { "risk", $"{_riskHost}/health" },
                { "notification", $"{_notificationHost}/health" },
            };

            bool allAvailable = true;

            foreach (var service in services)
            {
                try
                {
                    AnsiConsole.Markup($"[grey]Checking [yellow]{service.Key}[/] service...[/]");
                    log.WriteLine($"Checking {service.Key} service at {service.Value}");

                    var response = await _httpClientFactory.GetClient(service.Key).GetAsync("/health");

                    if (response.IsSuccessStatusCode)
                    {
                        AnsiConsole.MarkupLine($" [green]Available[/]");
                        log.WriteLine($"  Result: Available (Status: {response.StatusCode})");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($" [red]Unavailable (Status: {response.StatusCode})[/]");
                        log.WriteLine($"  Result: Unavailable (Status: {response.StatusCode})");
                        allAvailable = false;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($" [red]Error: {ex.Message}[/]");
                    log.WriteLine($"  Error: {ex.Message}");
                    allAvailable = false;
                }
            }

            return allAvailable;
        }

        /// <summary>
        /// Logs simulation action to file and optionally console
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="verbose">Whether to log to console as well</param>
        private Task LogActionAsync(string message, bool verbose = false)
        {
            lock (_simLogLock)
            {
                using var logWriter = new StreamWriter(_logFile, true);
                logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }

            if (verbose)
                AnsiConsole.MarkupLine($"[grey]{message}[/]");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates the simulation infrastructure
        /// </summary>
        private void CreateInfrastructure()
        {
            _totalUsers = _numUsers;
            _ordersPerUser = _numOrders;

            // Create HTTP client factory with timeout from configuration
            _httpClientFactory = new HttpClientFactory();
            _httpClientFactory.Configure(_timeout);

            // Create services factory with configured hosts
            var serviceUrls = new Dictionary<string, string>
            {
                { "identity", _identityHost },
                { "trading", _tradingHost },
                { "market-data", _marketDataHost },
                { "account", _accountHost },
                { "risk", _riskHost },
                { "notification", _notificationHost }
            };
            _httpClientFactory.ConfigureServiceUrls(serviceUrls);

            // Create user service
            var userService = new UserService(_identityHost, _httpClientFactory);

            // Register users
            _users = userService.CreateUsersAsync(_numUsers, _verbose).GetAwaiter().GetResult();

            // Create order service
            if (_users.Count > 0)
            {
                _orderService = new OrderService(
                    _httpClientFactory,
                    _totalUsers,
                    _ordersPerUser,
                    _symbolList,
                    _priceRanges,
                    _baseDelay,
                    _requestDelay);
            }
        }

        /// <summary>
        /// Checks the health of required services
        /// </summary>
        /// <returns>True if all services are healthy, false otherwise</returns>
        private async Task<bool> CheckServicesHealthAsync()
        {
            using var log = new StreamWriter(_logFile, true);
            log.WriteLine($"Checking service health at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var services = new Dictionary<string, string>
            {
                { "identity", $"{_identityHost}/health" },
                { "trading", $"{_tradingHost}/health" },
                { "market-data", $"{_marketDataHost}/health" },
                { "account", $"{_accountHost}/health" },
                { "risk", $"{_riskHost}/health" },
                { "notification", $"{_notificationHost}/health" },
            };

            bool allAvailable = true;

            foreach (var service in services)
            {
                // Skip match-making service connectivity check
                if (service.Key == "match-making")
                {
                    AnsiConsole.MarkupLine($"[grey]Service [yellow]{service.Key}[/]: [green]Available[/] (connectivity check skipped)[/]");
                    log.WriteLine($"Checking {service.Key} service: Available (connectivity check skipped)");
                    continue;
                }

                try
                {
                    AnsiConsole.Markup($"[grey]Checking [yellow]{service.Key}[/] service...[/]");
                    log.WriteLine($"Checking {service.Key} service at {service.Value}");

                    var response = await _httpClientFactory.GetClient(service.Key).GetAsync("/health");

                    if (response.IsSuccessStatusCode)
                    {
                        AnsiConsole.MarkupLine($" [green]Available[/]");
                        log.WriteLine($"  Result: Available (Status: {response.StatusCode})");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($" [red]Unavailable (Status: {response.StatusCode})[/]");
                        log.WriteLine($"  Result: Unavailable (Status: {response.StatusCode})");
                        allAvailable = false;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($" [red]Error: {ex.Message}[/]");
                    log.WriteLine($"  Error: {ex.Message}");
                    allAvailable = false;
                }
            }

            return allAvailable;
        }
    }
}