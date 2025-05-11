using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SimulationTest.Helpers;
using Spectre.Console;

namespace SimulationTest.Core
{
    /// <summary>
    /// Custom test attribute to mark test methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ApiTestAttribute : Attribute
    {
        public string Description { get; }
        public bool Skip { get; set; }
        public string SkipReason { get; set; }
        public string[] Dependencies { get; set; }

        public ApiTestAttribute(string description = null)
        {
            Description = description;
            Dependencies = Array.Empty<string>();
        }
    }

    /// <summary>
    /// API test result class
    /// </summary>
    public class ApiTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public TimeSpan Duration { get; set; }

        public static ApiTestResult Passed(TimeSpan duration) => new ApiTestResult
        {
            Success = true,
            Message = "Test passed successfully",
            Duration = duration
        };

        public static ApiTestResult Failed(string message, Exception ex, TimeSpan duration) => new ApiTestResult
        {
            Success = false,
            Message = message,
            Exception = ex,
            Duration = duration
        };

        public static ApiTestResult Skipped(string reason) => new ApiTestResult
        {
            Success = false,
            Message = $"Test skipped: {reason}"
        };
    }

    /// <summary>
    /// Manages the execution of unit tests
    /// </summary>
    public class UnitTestRunner
    {
        private readonly IConfiguration _configuration;
        private readonly string _logFile;
        private readonly bool _verbose;
        private readonly bool _generateCoverage;
        private readonly string _coverageOutputPath;
        private readonly int _testTimeout;
        private readonly int _retryCount;
        private TestProgressTracker _progressTracker;

        /// <summary>
        /// Initializes a new instance of the UnitTestRunner class
        /// </summary>
        /// <param name="configuration">Configuration for the test runner</param>
        public UnitTestRunner(IConfiguration configuration)
        {
            Console.WriteLine("Initializing UnitTestRunner...");
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Set up log file
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory("logs");
            _logFile = Path.Combine("logs", $"unittest_log_{timestamp}.txt");
            Console.WriteLine($"Log file created at: {_logFile}");

            // Initialize the log file
            using var logWriter = new StreamWriter(_logFile, false);
            logWriter.WriteLine("=== Trading System Unit Test Log ===");
            logWriter.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine("=================================\n");

            // Log the configuration to help debug issues
            Console.WriteLine("Logging configuration settings...");
            logWriter.WriteLine("Configuration Settings:");
            logWriter.WriteLine($"Tests:Mode = {configuration["Tests:Mode"] ?? "not set"}");
            logWriter.WriteLine($"Tests:Verbose = {configuration["Tests:Verbose"] ?? "not set"}");
            logWriter.WriteLine($"Tests:GenerateTestCoverage = {configuration["Tests:GenerateTestCoverage"] ?? "not set"}");
            logWriter.WriteLine($"Tests:CoverageOutputPath = {configuration["Tests:CoverageOutputPath"] ?? "not set"}");
            logWriter.WriteLine($"Tests:TestTimeout = {configuration["Tests:TestTimeout"] ?? "not set"}");
            logWriter.WriteLine($"Tests:RetryCount = {configuration["Tests:RetryCount"] ?? "not set"}");
            logWriter.WriteLine();

            // Get configuration settings (with safe parsing)
            _verbose = TryParseBool(configuration["Tests:Verbose"], false);
            _generateCoverage = TryParseBool(configuration["Tests:GenerateTestCoverage"], true);
            _coverageOutputPath = configuration["Tests:CoverageOutputPath"] ?? "./logs/coverage";
            _testTimeout = TryParseInt(configuration["Tests:TestTimeout"], 30);
            _retryCount = TryParseInt(configuration["Tests:RetryCount"], 3);

            // Ensure coverage output directory exists
            if (_generateCoverage)
            {
                Directory.CreateDirectory(_coverageOutputPath);
                Console.WriteLine($"Created coverage output directory: {_coverageOutputPath}");
            }

            // Log the effective configuration values
            Console.WriteLine("Effective configuration values:");
            Console.WriteLine($"  Verbose: {_verbose}");
            Console.WriteLine($"  GenerateTestCoverage: {_generateCoverage}");
            Console.WriteLine($"  CoverageOutputPath: {_coverageOutputPath}");
            Console.WriteLine($"  TestTimeout: {_testTimeout}");
            Console.WriteLine($"  RetryCount: {_retryCount}");

            logWriter.WriteLine("Effective Configuration Values:");
            logWriter.WriteLine($"Verbose: {_verbose}");
            logWriter.WriteLine($"GenerateTestCoverage: {_generateCoverage}");
            logWriter.WriteLine($"CoverageOutputPath: {_coverageOutputPath}");
            logWriter.WriteLine($"TestTimeout: {_testTimeout}");
            logWriter.WriteLine($"RetryCount: {_retryCount}");
            logWriter.WriteLine();

            // Initialize progress tracker (will be set with totalTests later)
            _progressTracker = new TestProgressTracker(0);
        }

        // Helper method for safe boolean parsing
        private bool TryParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        // Helper method for safe integer parsing
        private int TryParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Runs all unit tests in the assembly
        /// </summary>
        /// <returns>Exit code (0 for success, non-zero for failure)</returns>
        public async Task<int> RunTestsAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Configuration values:");
            AnsiConsole.WriteLine($"  Verbose: {_verbose}");
            AnsiConsole.WriteLine($"  GenerateTestCoverage: {_generateCoverage}");
            AnsiConsole.WriteLine($"  CoverageOutputPath: {_coverageOutputPath}");
            AnsiConsole.WriteLine($"  TestTimeout: {_testTimeout}");
            AnsiConsole.WriteLine($"  RetryCount: {_retryCount}");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[green]Running unit tests for Trading System API...[/]");
            Console.WriteLine("Starting test discovery and execution...");

            try
            {
                // Using reflection to find test methods
                var assemblyName = Assembly.GetExecutingAssembly().Location;
                Console.WriteLine($"Using assembly at: {assemblyName}");

                // Log the location to the log file
                using (var logWriter = new StreamWriter(_logFile, true))
                {
                    logWriter.WriteLine($"Test Assembly: {assemblyName}");
                    logWriter.WriteLine();
                }

                var testAssembly = Assembly.LoadFrom(assemblyName);
                Console.WriteLine("Assembly loaded successfully.");

                // Find all test classes (those with methods decorated with ApiTestAttribute)
                Console.WriteLine("Discovering test classes...");
                var testClasses = testAssembly.GetTypes()
                    .Where(t => t.GetMethods().Any(m =>
                        m.GetCustomAttributes(typeof(ApiTestAttribute), false).Length > 0))
                    .ToList();

                Console.WriteLine($"Discovered {testClasses.Count} test classes:");
                foreach (var testClass in testClasses)
                {
                    Console.WriteLine($"  - {testClass.Name}");
                }

                // Write test classes to log file
                using (var logWriter = new StreamWriter(_logFile, true))
                {
                    logWriter.WriteLine($"Discovered {testClasses.Count} test classes:");
                    foreach (var testClass in testClasses)
                    {
                        logWriter.WriteLine($"  - {testClass.Name}");
                    }
                    logWriter.WriteLine();
                }

                if (testClasses.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Warning: No test classes found in the assembly.[/]");
                    using var logWriter = new StreamWriter(_logFile, true);
                    logWriter.WriteLine("Warning: No test classes found in the assembly.");
                    return 0;
                }

                AnsiConsole.MarkupLine($"[blue]Found {testClasses.Count} test classes.[/]");

                // Collect all test methods
                var allTestMethods = new List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)>();
                foreach (var testClass in testClasses)
                {
                    foreach (var method in testClass.GetMethods())
                    {
                        var apiTestAttr = method.GetCustomAttribute<ApiTestAttribute>();
                        if (apiTestAttr != null)
                        {
                            allTestMethods.Add((testClass, method, apiTestAttr));
                        }
                    }
                }

                // Set up the progress tracker with the total number of tests
                int totalTests = allTestMethods.Count;
                _progressTracker = new TestProgressTracker(totalTests);

                int passedTests = 0;
                int failedTests = 0;
                int skippedTests = 0;

                // Start tracking progress
                _progressTracker.StartTracking();

                // Sort test methods based on dependencies
                var orderedTests = OrderTestsByDependencies(allTestMethods);

                // Check connectivity to services before running tests
                var httpClientFactory = new HttpClientFactory();
                httpClientFactory.Configure(
                    int.Parse(_configuration["TestSettings:TestTimeout"] ?? "30"));

                // Configure service URLs
                var serviceUrls = new Dictionary<string, string>
                {
                    { "identity", _configuration["SimulationSettings:IdentityHost"] ?? "http://identity.trading-system.local" },
                    { "trading", _configuration["SimulationSettings:TradingHost"] ?? "http://trading.trading-system.local" },
                    { "market-data", _configuration["SimulationSettings:MarketDataHost"] ?? "http://market-data.trading-system.local" },
                    { "account", _configuration["SimulationSettings:AccountHost"] ?? "http://account.trading-system.local" },
                    { "risk", _configuration["SimulationSettings:RiskHost"] ?? "http://risk.trading-system.local" },
                    { "notification", _configuration["SimulationSettings:NotificationHost"] ?? "http://notification.trading-system.local" },
                    { "match-making", _configuration["SimulationSettings:MatchMakingHost"] ?? "http://match-making.trading-system.local" }
                };

                httpClientFactory.ConfigureServiceUrls(serviceUrls);

                var connectivityChecker = new ServiceConnectivityChecker(httpClientFactory, serviceUrls);
                await connectivityChecker.CheckAllServicesAsync();

                // Execute tests in order
                foreach (var (classType, method, attribute) in orderedTests)
                {
                    Console.WriteLine($"Running test: {classType.Name}.{method.Name} - {attribute.Description ?? "No description"}");
                    using (var logWriter = new StreamWriter(_logFile, true))
                    {
                        logWriter.WriteLine($"  Test: {method.Name}");
                        logWriter.WriteLine($"  Description: {attribute.Description ?? "No description"}");
                        logWriter.WriteLine($"  Start time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    }

                    // Check if test is skipped
                    if (attribute.Skip)
                    {
                        skippedTests++;
                        AnsiConsole.MarkupLine($"[yellow]SKIPPED[/]: {classType.Name}.{method.Name}");
                        using (var logWriter = new StreamWriter(_logFile, true))
                        {
                            logWriter.WriteLine($"  Result: SKIPPED");
                            logWriter.WriteLine($"  Skip reason: {attribute.SkipReason ?? "No reason provided"}");
                            logWriter.WriteLine();
                        }
                        continue;
                    }

                    try
                    {
                        // Create instance of the test class
                        Console.WriteLine($"  Creating instance of {classType.Name}");
                        var instance = Activator.CreateInstance(classType);
                        if (instance == null)
                        {
                            throw new InvalidOperationException($"Could not create instance of test class {classType.Name}");
                        }

                        // Execute the test method with retry logic
                        ApiTestResult testResult = null;
                        Exception lastException = null;

                        for (int attempt = 0; attempt < _retryCount && (testResult == null || !testResult.Success); attempt++)
                        {
                            if (attempt > 0)
                            {
                                Console.WriteLine($"  Retry attempt {attempt} of {_retryCount}");
                                AnsiConsole.MarkupLine($"[yellow]RETRY[/] {attempt} of {_retryCount}: {classType.Name}.{method.Name}");
                                using (var logWriter = new StreamWriter(_logFile, true))
                                {
                                    logWriter.WriteLine($"  Retry attempt {attempt} of {_retryCount}");
                                }
                            }

                            try
                            {
                                // Create a cancellation token source with the configured timeout
                                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(_testTimeout));
                                Console.WriteLine($"  Executing test with timeout: {_testTimeout} seconds");

                                // Run the test with a timeout
                                var methodStopwatch = new Stopwatch();
                                methodStopwatch.Start();

                                // Special handling for async methods
                                var task = Task.Run(() =>
                                {
                                    if (method.ReturnType == typeof(Task<ApiTestResult>))
                                    {
                                        // For async methods returning Task<ApiTestResult>
                                        var resultTask = (Task<ApiTestResult>)method.Invoke(instance, null);
                                        return resultTask.Result;
                                    }
                                    else if (method.ReturnType == typeof(Task))
                                    {
                                        // For async methods returning Task, consider as success
                                        var voidTask = (Task)method.Invoke(instance, null);
                                        voidTask.Wait();
                                        return ApiTestResult.Passed(methodStopwatch.Elapsed);
                                    }
                                    else if (method.ReturnType == typeof(ApiTestResult))
                                    {
                                        // For synchronous methods, return result directly
                                        return (ApiTestResult)method.Invoke(instance, null);
                                    }
                                    else
                                    {
                                        // For other return types, consider as success
                                        method.Invoke(instance, null);
                                        return ApiTestResult.Passed(methodStopwatch.Elapsed);
                                    }
                                });

                                await task.WaitAsync(cts.Token);
                                methodStopwatch.Stop();

                                testResult = task.Result;
                                if (testResult.Success)
                                {
                                    Console.WriteLine($"  Test executed successfully");
                                    break;
                                }
                                else
                                {
                                    lastException = testResult.Exception;
                                    Console.WriteLine($"  Test failed: {testResult.Message}");
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"  Test timed out after {_testTimeout} seconds");
                                lastException = new TimeoutException($"Test timed out after {_testTimeout} seconds");
                                using (var logWriter = new StreamWriter(_logFile, true))
                                {
                                    logWriter.WriteLine($"  Execution timed out after {_testTimeout} seconds");
                                }
                            }
                            catch (Exception ex)
                            {
                                var actualException = ex.InnerException ?? ex;
                                Console.WriteLine($"  Test failed with exception: {actualException.GetType().Name}: {actualException.Message}");
                                lastException = actualException;
                                using (var logWriter = new StreamWriter(_logFile, true))
                                {
                                    logWriter.WriteLine($"  Execution failed with exception: {actualException.GetType().Name}");
                                    logWriter.WriteLine($"  Message: {actualException.Message}");
                                    logWriter.WriteLine($"  Stack trace: {actualException.StackTrace}");
                                }

                                // Only wait before retrying if this isn't the last attempt
                                if (attempt < _retryCount - 1)
                                {
                                    await Task.Delay(500 * (attempt + 1)); // Exponential backoff
                                }
                            }
                        }

                        if (testResult != null && testResult.Success)
                        {
                            passedTests++;
                            AnsiConsole.MarkupLine($"[green]PASSED[/]: {classType.Name}.{method.Name}");
                            Console.WriteLine($"  Result: PASSED");
                            using (var logWriter = new StreamWriter(_logFile, true))
                            {
                                logWriter.WriteLine($"  Result: PASSED");
                                logWriter.WriteLine($"  End time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                logWriter.WriteLine($"  Duration: {testResult.Duration.TotalMilliseconds:F2} ms");
                                logWriter.WriteLine();
                            }
                        }
                        else
                        {
                            failedTests++;
                            AnsiConsole.MarkupLine($"[red]FAILED[/]: {classType.Name}.{method.Name}");
                            Console.WriteLine($"  Result: FAILED");

                            string errorMessage = testResult?.Message ??
                                (lastException != null ? lastException.Message : "Unknown error");

                            if (_verbose)
                            {
                                AnsiConsole.MarkupLine($"[red]Error: {errorMessage}[/]");
                            }

                            // Log the failure
                            using var logWriter = new StreamWriter(_logFile, true);
                            logWriter.WriteLine($"  Result: FAILED");
                            logWriter.WriteLine($"  End time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            logWriter.WriteLine($"  Error: {errorMessage}");
                            if (lastException != null)
                            {
                                logWriter.WriteLine($"  Stack trace: {lastException.StackTrace}");
                            }
                            logWriter.WriteLine();
                        }

                        // Update progress tracker
                        _progressTracker.UpdateTestResult(testResult ?? ApiTestResult.Failed("Failed to get test result", lastException, TimeSpan.Zero));
                    }
                    catch (Exception ex)
                    {
                        failedTests++;
                        AnsiConsole.MarkupLine($"[red]ERROR[/]: {classType.Name}.{method.Name}");
                        AnsiConsole.MarkupLine($"[red]Could not instantiate test class: {ex.Message}[/]");
                        Console.WriteLine($"  ERROR: Could not instantiate test class: {ex.Message}");

                        // Log the failure
                        using var logWriter = new StreamWriter(_logFile, true);
                        logWriter.WriteLine($"  Result: ERROR (Could not instantiate test class)");
                        logWriter.WriteLine($"  End time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        logWriter.WriteLine($"  Error: {ex.Message}");
                        logWriter.WriteLine($"  Stack trace: {ex.StackTrace}");
                        logWriter.WriteLine();

                        // Update progress tracker with failure
                        _progressTracker.UpdateTestResult(ApiTestResult.Failed($"Could not instantiate test class: {ex.Message}", ex, TimeSpan.Zero));
                    }
                }

                // Stop tracking progress
                _progressTracker.StopTracking();

                // Display summary
                _progressTracker.RenderSummary();

                stopwatch.Stop();
                var elapsed = stopwatch.Elapsed;

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[blue]Test Run Complete.[/]");
                AnsiConsole.WriteLine($"Time elapsed: {elapsed.TotalSeconds:F2} seconds");
                AnsiConsole.WriteLine($"Total tests: {totalTests}");
                AnsiConsole.WriteLine($"Passed: {passedTests}");
                AnsiConsole.WriteLine($"Failed: {failedTests}");
                AnsiConsole.WriteLine($"Skipped: {skippedTests}");
                AnsiConsole.WriteLine();

                if (failedTests > 0)
                {
                    AnsiConsole.MarkupLine($"[red]Failed test count: {failedTests}[/]");
                    AnsiConsole.MarkupLine($"[yellow]See log file for details: {_logFile}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine("[green]All tests passed.[/]");
                AnsiConsole.WriteLine();
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error running tests: {ex.Message}[/]");
                Console.WriteLine($"Error running tests: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                using var logWriter = new StreamWriter(_logFile, true);
                logWriter.WriteLine("UNHANDLED EXCEPTION");
                logWriter.WriteLine($"Error: {ex.Message}");
                logWriter.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        /// <summary>
        /// Orders test methods based on dependencies
        /// </summary>
        /// <param name="testMethods">List of test methods</param>
        /// <returns>Ordered list of test methods</returns>
        private List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)> OrderTestsByDependencies(
            List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)> testMethods)
        {
            var result = new List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)>();
            var processed = new HashSet<string>();

            // First add methods without dependencies
            foreach (var test in testMethods.Where(t => t.Attribute.Dependencies.Length == 0))
            {
                result.Add(test);
                processed.Add($"{test.ClassType.FullName}.{test.Method.Name}");
            }

            // Then add methods with dependencies in order
            while (result.Count < testMethods.Count)
            {
                bool addedAny = false;

                foreach (var test in testMethods)
                {
                    string testName = $"{test.ClassType.FullName}.{test.Method.Name}";

                    if (processed.Contains(testName))
                        continue;

                    bool allDependenciesSatisfied = true;
                    foreach (var dependency in test.Attribute.Dependencies)
                    {
                        if (!processed.Contains(dependency))
                        {
                            allDependenciesSatisfied = false;
                            break;
                        }
                    }

                    if (allDependenciesSatisfied)
                    {
                        result.Add(test);
                        processed.Add(testName);
                        addedAny = true;
                    }
                }

                if (!addedAny)
                {
                    // If we couldn't add any tests this round, there might be a dependency cycle
                    // Add the remaining tests in any order
                    foreach (var test in testMethods)
                    {
                        string testName = $"{test.ClassType.FullName}.{test.Method.Name}";
                        if (!processed.Contains(testName))
                        {
                            result.Add(test);
                            processed.Add(testName);
                        }
                    }
                }
            }

            return result;
        }
    }
}