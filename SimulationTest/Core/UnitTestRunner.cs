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
        public string TestName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public TimeSpan Duration { get; set; }

        public static ApiTestResult Passed(string testName, TimeSpan duration) => new ApiTestResult
        {
            TestName = testName,
            Success = true,
            Message = "Test passed successfully",
            Duration = duration
        };

        public static ApiTestResult Failed(string testName, string message, Exception ex, TimeSpan duration) => new ApiTestResult
        {
            TestName = testName,
            Success = false,
            Message = message,
            Exception = ex,
            Duration = duration
        };

        public static ApiTestResult Skipped(string testName, string reason) => new ApiTestResult
        {
            TestName = testName,
            Success = false,
            Message = $"Test skipped: {reason}"
        };
    }

    /// <summary>
    /// Test run results data structure
    /// </summary>
    public class TestRunResults
    {
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public TimeSpan Elapsed { get; set; }
        public List<TimeSpan> TestLatencies { get; set; } = new List<TimeSpan>();
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
        private readonly string _timestamp;
        private readonly string _testFolderPath;
        private TestProgressTracker _progressTracker;
        private readonly IProgress<TestProgress> _progressReporter;

        /// <summary>
        /// Initializes a new instance of the UnitTestRunner class
        /// </summary>
        /// <param name="configuration">Configuration for the test runner</param>
        /// <param name="progressReporter">Reporter for real-time progress updates</param>
        public UnitTestRunner(IConfiguration configuration, IProgress<TestProgress> progressReporter = null)
        {
            _progressReporter = progressReporter;
            ReportProgress("Initializing UnitTestRunner...", 0, 0, 0, 0, 0, 0, "Initializing UnitTestRunner...");

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Create timestamp for consistent naming across all files
            _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Create a dedicated folder for this test run
            _testFolderPath = Path.Combine("logs", $"unit_test_{_timestamp}");
            Directory.CreateDirectory(_testFolderPath);

            // Set up log file within the test folder
            _logFile = configuration["TestSettings:LogFile"] ?? Path.Combine(_testFolderPath, "unittest_log.txt");
            ReportProgress("Initializing", 0, 0, 0, 0, 0, 0, $"Log file created at: {_logFile}");

            // Get configuration settings (with safe parsing)
            _verbose = TryParseBool(configuration["Tests:Verbose"], false);
            _generateCoverage = TryParseBool(configuration["Tests:GenerateTestCoverage"], true);
            _coverageOutputPath = configuration["Tests:CoverageOutputPath"] ?? "./logs/coverage";
            _testTimeout = TryParseInt(configuration["TestSettings:TestTimeout"], 30);
            _retryCount = TryParseInt(configuration["Tests:RetryCount"], 3);

            // Initialize progress tracker with log file - only this object will directly write to the log file
            _progressTracker = new TestProgressTracker(0, 500, _progressReporter, _logFile);

            // Log initial configuration via the progress tracker
            _progressTracker.LogMessage("=== Trading System Unit Test Log ===");
            _progressTracker.LogMessage($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _progressTracker.LogMessage("=================================");
            _progressTracker.LogMessage("");

            // Log the configuration to help debug issues
            _progressTracker.LogMessage("Configuration Settings:");
            _progressTracker.LogMessage($"Tests:Mode = {configuration["Tests:Mode"] ?? "not set"}");
            _progressTracker.LogMessage($"Tests:Verbose = {configuration["Tests:Verbose"] ?? "not set"}");
            _progressTracker.LogMessage($"Tests:GenerateTestCoverage = {configuration["Tests:GenerateTestCoverage"] ?? "not set"}");
            _progressTracker.LogMessage($"Tests:CoverageOutputPath = {configuration["Tests:CoverageOutputPath"] ?? "not set"}");
            _progressTracker.LogMessage($"Tests:TestTimeout = {configuration["Tests:TestTimeout"] ?? "not set"}");
            _progressTracker.LogMessage($"Tests:RetryCount = {configuration["Tests:RetryCount"] ?? "not set"}");
            _progressTracker.LogMessage("");

            // Ensure coverage output directory exists
            if (_generateCoverage)
            {
                Directory.CreateDirectory(_coverageOutputPath);
                ReportProgress("Setup", 0, 0, 0, 0, 0, 0, $"Created coverage output directory: {_coverageOutputPath}");
            }

            // Log the effective configuration values
            var configMessage = "Effective configuration values:" +
                $"\n  Verbose: {_verbose}" +
                $"\n  GenerateTestCoverage: {_generateCoverage}" +
                $"\n  CoverageOutputPath: {_coverageOutputPath}" +
                $"\n  TestTimeout: {_testTimeout}" +
                $"\n  RetryCount: {_retryCount}";

            ReportProgress("Configuration", 0, 0, 0, 0, 0, 0, configMessage);

            _progressTracker.LogMessage("Effective Configuration Values:");
            _progressTracker.LogMessage($"Verbose: {_verbose}");
            _progressTracker.LogMessage($"GenerateTestCoverage: {_generateCoverage}");
            _progressTracker.LogMessage($"CoverageOutputPath: {_coverageOutputPath}");
            _progressTracker.LogMessage($"TestTimeout: {_testTimeout}");
            _progressTracker.LogMessage($"RetryCount: {_retryCount}");
            _progressTracker.LogMessage("");
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
        /// Reports progress through the progress reporter
        /// </summary>
        private void ReportProgress(string message, int percentage, int completed, int total, int passed = 0, int failed = 0, int skipped = 0, string logMessage = null)
        {
            if (_progressReporter != null)
            {
                _progressReporter.Report(new TestProgress
                {
                    Message = message,
                    Percentage = percentage,
                    Completed = completed,
                    Total = total,
                    Passed = passed,
                    Failed = failed,
                    Skipped = skipped,
                    LogMessage = logMessage
                });
            }

            // Also log to console
            if (!string.IsNullOrEmpty(logMessage))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {logMessage}");
            }
        }

        /// <summary>
        /// Runs all unit tests in the assembly
        /// </summary>
        /// <returns>Test run results with statistics</returns>
        public async Task<TestRunResults> RunTestsAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ReportProgress("Starting tests", 0, 0, 0, 0, 0, 0, "Running unit tests for Trading System API...");

            // Initialize test counts
            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;
            int skippedTests = 0;

            try
            {
                // Using reflection to find test methods
                var assemblyName = Assembly.GetExecutingAssembly().Location;
                ReportProgress("Loading assembly", 0, 0, 0, 0, 0, 0, $"Using assembly at: {assemblyName}");

                // Log the location to the log file via progress tracker
                _progressTracker.LogMessage($"Test Assembly: {assemblyName}");
                _progressTracker.LogMessage("");

                var testAssembly = Assembly.LoadFrom(assemblyName);
                ReportProgress("Assembly loaded", 0, 0, 0, 0, 0, 0, "Assembly loaded successfully.");

                // Find all test classes (those with methods decorated with ApiTestAttribute)
                ReportProgress("Discovering test classes", 0, 0, 0, 0, 0, 0, "Discovering test classes...");
                var testClasses = testAssembly.GetTypes()
                    .Where(t => t.GetMethods().Any(m =>
                        m.GetCustomAttributes(typeof(ApiTestAttribute), false).Length > 0))
                    .ToList();

                ReportProgress("Test classes discovered", 0, 0, 0, 0, 0, 0, $"Discovered {testClasses.Count} test classes:");
                foreach (var testClass in testClasses)
                {
                    ReportProgress("Test class discovered", 0, 0, 0, 0, 0, 0, $"  - {testClass.Name}");
                }

                // Write test classes to log file via progress tracker
                _progressTracker.LogMessage($"Discovered {testClasses.Count} test classes:");
                foreach (var testClass in testClasses)
                {
                    _progressTracker.LogMessage($"  - {testClass.Name}");
                }
                _progressTracker.LogMessage("");

                if (testClasses.Count == 0)
                {
                    ReportProgress("Warning", 0, 0, 0, 0, 0, 0, "Warning: No test classes found in the assembly.");
                    _progressTracker.LogMessage("Warning: No test classes found in the assembly.");
                    return new TestRunResults
                    {
                        Total = totalTests,
                        Passed = passedTests,
                        Failed = failedTests,
                        Skipped = skippedTests,
                        Elapsed = stopwatch.Elapsed
                    };
                }

                ReportProgress("Test classes found", 0, 0, 0, 0, 0, 0, $"[blue]Found {testClasses.Count} test classes.[/]");

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
                totalTests = allTestMethods.Count;
                _progressTracker = new TestProgressTracker(totalTests);

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
                    ReportProgress("Running test", 0, 0, 0, 0, 0, 0, $"Running test: {classType.Name}.{method.Name} - {attribute.Description ?? "No description"}");
                    _progressTracker.LogMessage($"  Test: {method.Name}");
                    _progressTracker.LogMessage($"  Description: {attribute.Description ?? "No description"}");
                    _progressTracker.LogMessage($"  Start time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                    // Check if test is skipped
                    if (attribute.Skip)
                    {
                        skippedTests++;
                        ReportProgress("Skipped", 0, 0, 0, 0, 0, 0, $"[yellow]SKIPPED[/]: {classType.Name}.{method.Name}");
                        _progressTracker.LogMessage($"  Result: SKIPPED");
                        _progressTracker.LogMessage($"  Skip reason: {attribute.SkipReason ?? "No reason provided"}");
                        _progressTracker.LogMessage("");
                        continue;
                    }

                    try
                    {
                        // Create instance of the test class
                        ReportProgress("Creating instance of test class", 0, 0, 0, 0, 0, 0, $"  Creating instance of {classType.Name}");
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
                                ReportProgress("Retry attempt", 0, 0, 0, 0, 0, 0, $"  Retry attempt {attempt} of {_retryCount}");
                                ReportProgress("Retry attempt", 0, 0, 0, 0, 0, 0, $"[yellow]RETRY[/] {attempt} of {_retryCount}: {classType.Name}.{method.Name}");
                                _progressTracker.LogMessage($"  Retry attempt {attempt} of {_retryCount}");
                            }

                            try
                            {
                                // Create a cancellation token source with the configured timeout
                                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(_testTimeout));
                                ReportProgress("Executing test", 0, 0, 0, 0, 0, 0, $"  Executing test with timeout: {_testTimeout} seconds");

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
                                        return ApiTestResult.Passed(method.Name, methodStopwatch.Elapsed);
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
                                        return ApiTestResult.Passed(method.Name, methodStopwatch.Elapsed);
                                    }
                                });

                                await task.WaitAsync(cts.Token);
                                methodStopwatch.Stop();

                                testResult = task.Result;
                                if (testResult.Success)
                                {
                                    ReportProgress("Test executed successfully", 0, 0, 0, 0, 0, 0, $"  Test executed successfully");
                                    break;
                                }
                                else
                                {
                                    lastException = testResult.Exception;
                                    ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"  Test failed: {testResult.Message}");
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                ReportProgress("Test timed out", 0, 0, 0, 0, 0, 0, $"  Test timed out after {_testTimeout} seconds");
                                lastException = new TimeoutException($"Test timed out after {_testTimeout} seconds");
                                _progressTracker.LogMessage($"  Execution timed out after {_testTimeout} seconds");
                            }
                            catch (Exception ex)
                            {
                                var actualException = ex.InnerException ?? ex;
                                ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"  Test failed with exception: {actualException.GetType().Name}: {actualException.Message}");
                                lastException = actualException;
                                _progressTracker.LogMessage($"  Execution failed with exception: {actualException.GetType().Name}");
                                _progressTracker.LogMessage($"  Message: {actualException.Message}");
                                _progressTracker.LogMessage($"  Stack trace: {actualException.StackTrace}");

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
                            ReportProgress("Test passed", 0, 0, 0, 0, 0, 0, $"[green]PASSED[/]: {classType.Name}.{method.Name}");
                            ReportProgress("Test passed", 0, 0, 0, 0, 0, 0, $"  Result: PASSED");
                            _progressTracker.LogMessage($"  Result: PASSED");
                            _progressTracker.LogMessage($"  End time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            _progressTracker.LogMessage($"  Duration: {testResult.Duration.TotalMilliseconds:F2} ms");
                            _progressTracker.LogMessage("");
                        }
                        else
                        {
                            failedTests++;
                            ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"[red]FAILED[/]: {classType.Name}.{method.Name}");
                            ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"  Result: FAILED");

                            string errorMessage = testResult?.Message ??
                                (lastException != null ? lastException.Message : "Unknown error");

                            if (_verbose)
                            {
                                ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"[red]Error: {errorMessage}[/]");
                            }

                            // Log the failure
                            _progressTracker.LogMessage($"  Result: FAILED");
                            _progressTracker.LogMessage($"  End time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            _progressTracker.LogMessage($"  Error: {errorMessage}");
                            if (lastException != null)
                            {
                                _progressTracker.LogMessage($"  Stack trace: {lastException.StackTrace}");
                            }
                            _progressTracker.LogMessage("");
                        }

                        // Update progress tracker with the test result
                        _progressTracker.UpdateTestResult(testResult ?? ApiTestResult.Failed(method.Name,
                            testResult?.Message ?? (lastException != null ? lastException.Message : "Unknown error"),
                            lastException,
                            TimeSpan.Zero));

                        // Explicitly report progress after each test completes
                        ReportProgress(
                            "Running tests",
                            (int)(100.0 * (passedTests + failedTests + skippedTests) / totalTests),
                            passedTests + failedTests + skippedTests,
                            totalTests,
                            passedTests,
                            failedTests,
                            skippedTests,
                            $"Test '{method.Name}' completed: {(testResult != null && testResult.Success ? "PASSED" : "FAILED")}"
                        );
                    }
                    catch (Exception ex)
                    {
                        failedTests++;
                        ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"[red]ERROR[/]: {classType.Name}.{method.Name}");
                        ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"[red]Could not instantiate test class: {ex.Message}[/]");
                        ReportProgress("Test failed", 0, 0, 0, 0, 0, 0, $"  ERROR: Could not instantiate test class: {ex.Message}");

                        // Log the failure
                        _progressTracker.LogMessage($"  Result: ERROR (Could not instantiate test class)");
                        _progressTracker.LogMessage($"  End time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        _progressTracker.LogMessage($"  Error: {ex.Message}");
                        _progressTracker.LogMessage($"  Stack trace: {ex.StackTrace}");
                        _progressTracker.LogMessage("");

                        // Update progress tracker with failure
                        _progressTracker.UpdateTestResult(ApiTestResult.Failed(method.Name, $"Could not instantiate test class: {ex.Message}", ex, TimeSpan.Zero));
                    }
                }

                // Stop tracking progress
                _progressTracker.StopTracking();

                // Display summary
                _progressTracker.RenderSummary();

                stopwatch.Stop();
                var elapsed = stopwatch.Elapsed;

                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, "Test Run Complete.");
                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, $"Time elapsed: {elapsed.TotalSeconds:F2} seconds");
                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, $"Total tests: {totalTests}");
                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, $"Passed: {passedTests}");
                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, $"Failed: {failedTests}");
                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, $"Skipped: {skippedTests}");
                ReportProgress("Test run complete", 0, 0, 0, 0, 0, 0, "");

                if (failedTests > 0)
                {
                    ReportProgress("Failed test count", 0, 0, 0, 0, 0, 0, $"[red]Failed test count: {failedTests}[/]");
                    ReportProgress("Failed test count", 0, 0, 0, 0, 0, 0, $"[yellow]See log file for details: {_logFile}[/]");

                    // Clean up coverage files
                    CleanupCoverageFiles();

                    return new TestRunResults
                    {
                        Total = totalTests,
                        Passed = passedTests,
                        Failed = failedTests,
                        Skipped = skippedTests,
                        Elapsed = elapsed,
                        TestLatencies = _progressTracker.GetLatencies().ToList()
                    };
                }

                ReportProgress("All tests passed", 0, 0, 0, 0, 0, 0, "[green]All tests passed.[/]");
                ReportProgress("All tests passed", 0, 0, 0, 0, 0, 0, "");

                // Clean up coverage files
                CleanupCoverageFiles();

                return new TestRunResults
                {
                    Total = totalTests,
                    Passed = passedTests,
                    Failed = failedTests,
                    Skipped = skippedTests,
                    Elapsed = elapsed,
                    TestLatencies = _progressTracker.GetLatencies().ToList()
                };
            }
            catch (Exception ex)
            {
                ReportProgress("Error", 100, totalTests, totalTests, passedTests, failedTests, skippedTests, $"Unhandled exception: {ex.Message}");
                _progressTracker.LogMessage("UNHANDLED EXCEPTION");
                _progressTracker.LogMessage($"Error: {ex.Message}");
                _progressTracker.LogMessage(ex.StackTrace);

                // Still try to clean up coverage files
                try
                {
                    CleanupCoverageFiles();
                }
                catch { }

                return new TestRunResults
                {
                    Total = totalTests,
                    Passed = passedTests,
                    Failed = failedTests,
                    Skipped = skippedTests,
                    Elapsed = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// Orders tests by their dependencies, ensuring that dependent tests are run after their dependencies
        /// </summary>
        private List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)> OrderTestsByDependencies(
            List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)> testMethods)
        {
            // Create a map of method names to test methods for easy lookup
            var testMethodMap = new Dictionary<string, (Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)>();
            foreach (var testMethod in testMethods)
            {
                string fullyQualifiedName = $"{testMethod.ClassType.FullName}.{testMethod.Method.Name}";
                testMethodMap[fullyQualifiedName] = testMethod;

                // Also add without namespace to support simpler dependency reference
                testMethodMap[$"{testMethod.ClassType.Name}.{testMethod.Method.Name}"] = testMethod;
            }

            // Log all available tests
            ReportProgress("Available tests for dependency resolution", 0, 0, 0, 0, 0, 0, "Available tests for dependency resolution:");
            foreach (var key in testMethodMap.Keys)
            {
                ReportProgress("Available tests for dependency resolution", 0, 0, 0, 0, 0, 0, $"  - {key}");
            }

            // Build a dependency graph
            var dependencyGraph = new Dictionary<string, List<string>>();
            foreach (var testMethod in testMethods)
            {
                string fullyQualifiedName = $"{testMethod.ClassType.FullName}.{testMethod.Method.Name}";

                if (testMethod.Attribute.Dependencies != null && testMethod.Attribute.Dependencies.Length > 0)
                {
                    dependencyGraph[fullyQualifiedName] = new List<string>(testMethod.Attribute.Dependencies);

                    // Log dependencies
                    ReportProgress("Test dependencies", 0, 0, 0, 0, 0, 0, $"Test {fullyQualifiedName} depends on:");
                    foreach (var dep in testMethod.Attribute.Dependencies)
                    {
                        ReportProgress("Test dependencies", 0, 0, 0, 0, 0, 0, $"  - {dep}");

                        // Check if the dependency exists
                        if (!testMethodMap.ContainsKey(dep))
                        {
                            ReportProgress("Test dependencies", 0, 0, 0, 0, 0, 0, $"WARNING: Dependency {dep} not found in available tests!");

                            // Try with namespace
                            string[] parts = dep.Split('.');
                            if (parts.Length >= 2)
                            {
                                string className = parts[parts.Length - 2];
                                string methodName = parts[parts.Length - 1];

                                var matchingTests = testMethods
                                    .Where(t => t.ClassType.Name == className && t.Method.Name == methodName)
                                    .ToList();

                                if (matchingTests.Any())
                                {
                                    ReportProgress("Test dependencies", 0, 0, 0, 0, 0, 0, $"  Found matching test without full namespace: {matchingTests.First().ClassType.FullName}.{matchingTests.First().Method.Name}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    dependencyGraph[fullyQualifiedName] = new List<string>();
                }
            }

            // Perform topological sort to get the ordered tests
            var visited = new HashSet<string>();
            var orderedTests = new List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)>();

            foreach (var testMethod in testMethods)
            {
                string fullyQualifiedName = $"{testMethod.ClassType.FullName}.{testMethod.Method.Name}";

                if (!visited.Contains(fullyQualifiedName))
                {
                    VisitNode(fullyQualifiedName, dependencyGraph, visited, orderedTests, testMethodMap, new HashSet<string>());
                }
            }

            // Log the ordered tests
            ReportProgress("Ordered tests", 0, 0, 0, 0, 0, 0, "Ordered tests:");
            for (int i = 0; i < orderedTests.Count; i++)
            {
                var testMethod = orderedTests[i];
                ReportProgress("Ordered tests", 0, 0, 0, 0, 0, 0, $"  {i + 1}. {testMethod.ClassType.FullName}.{testMethod.Method.Name}");
            }

            return orderedTests;
        }

        /// <summary>
        /// Visits a node in the dependency graph and adds it to the ordered list after its dependencies
        /// </summary>
        private void VisitNode(
            string node,
            Dictionary<string, List<string>> dependencyGraph,
            HashSet<string> visited,
            List<(Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)> orderedTests,
            Dictionary<string, (Type ClassType, MethodInfo Method, ApiTestAttribute Attribute)> testMethodMap,
            HashSet<string> currentPath)
        {
            if (currentPath.Contains(node))
            {
                // We have a circular dependency
                ReportProgress("Circular dependency", 0, 0, 0, 0, 0, 0, $"WARNING: Circular dependency detected involving {node}");
                return;
            }

            if (visited.Contains(node))
            {
                // Already processed
                return;
            }

            currentPath.Add(node);

            if (dependencyGraph.ContainsKey(node))
            {
                foreach (var dependency in dependencyGraph[node])
                {
                    if (testMethodMap.ContainsKey(dependency))
                    {
                        VisitNode(dependency, dependencyGraph, visited, orderedTests, testMethodMap, currentPath);
                    }
                    else
                    {
                        // Missing dependency - see if we can resolve it by class name
                        string[] parts = dependency.Split('.');
                        if (parts.Length >= 2)
                        {
                            string className = parts[parts.Length - 2];
                            string methodName = parts[parts.Length - 1];

                            var match = testMethodMap.FirstOrDefault(x =>
                                x.Key.EndsWith($"{className}.{methodName}"));

                            if (!string.IsNullOrEmpty(match.Key))
                            {
                                ReportProgress("Resolved dependency", 0, 0, 0, 0, 0, 0, $"Resolved dependency {dependency} to {match.Key}");
                                VisitNode(match.Key, dependencyGraph, visited, orderedTests, testMethodMap, currentPath);
                            }
                            else
                            {
                                ReportProgress("Error", 0, 0, 0, 0, 0, 0, $"ERROR: Required dependency {dependency} not found!");
                            }
                        }
                    }
                }
            }

            visited.Add(node);
            currentPath.Remove(node);

            if (testMethodMap.ContainsKey(node))
            {
                orderedTests.Add(testMethodMap[node]);
            }
        }

        /// <summary>
        /// Cleans up coverage files after test run
        /// </summary>
        private void CleanupCoverageFiles()
        {
            if (_generateCoverage)
            {
                try
                {
                    // Only delete the contents, keep the directory structure
                    foreach (var file in Directory.GetFiles(_coverageOutputPath))
                    {
                        File.Delete(file);
                    }
                    foreach (var dir in Directory.GetDirectories(_coverageOutputPath))
                    {
                        Directory.Delete(dir, true);
                    }
                    ReportProgress("Coverage files cleaned up", 0, 0, 0, 0, 0, 0, "Coverage files cleaned up.");
                }
                catch (Exception ex)
                {
                    ReportProgress("Warning", 0, 0, 0, 0, 0, 0, $"Warning: Could not clean up coverage files: {ex.Message}");
                }
            }
        }
    }
}