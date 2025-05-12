using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimulationTest.Core;
using SimulationTest.Helpers;
using Spectre.Console;
using System.Reflection;

namespace SimulationTest
{
    public class Program
    {
        private static IConfiguration _configuration;
        private static IServiceProvider _serviceProvider;

        public static async Task Main(string[] args)
        {
            // Setup configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Run the application main loop
            bool exitRequested = false;
            while (!exitRequested)
            {
                ShowMainMenu();
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Please select a test type:")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Stress Test",
                            "Unit Test",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "Stress Test":
                        await RunStressTest();
                        break;
                    case "Unit Test":
                        await RunUnitTest();
                        break;
                    case "Exit":
                        exitRequested = true;
                        break;
                }

                if (!exitRequested)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Press any key to return to the main menu...[/]");
                    Console.ReadKey(true);
                    Console.Clear();
                }
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            services.AddSingleton(_configuration);

            // Register application services
            services.AddSingleton<HttpClientFactory>();
            services.AddScoped<ServiceConnectivityChecker>();

            // Configure JSON serialization options
            services.AddSingleton(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        private static void ShowMainMenu()
        {
            Console.Clear();
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            AnsiConsole.Write(
                new FigletText("Trading System")
                    .LeftJustified()
                    .Color(Color.Green));

            AnsiConsole.Write(
                new FigletText("Test Console")
                    .LeftJustified()
                    .Color(Color.Yellow));

            AnsiConsole.MarkupLine($"[grey]Version {version}[/]");
            AnsiConsole.WriteLine();
        }

        private static async Task RunStressTest()
        {
            Console.Clear();
            AnsiConsole.Write(new Rule("[yellow]Stress Test Configuration[/]").RuleStyle("grey").LeftJustified());
            AnsiConsole.WriteLine();

            // Use default values without prompting
            int userCount = 20;
            int ordersPerUser = 1000;
            int concurrency = 10;
            int timeoutSeconds = int.Parse(_configuration["TestSettings:TestTimeout"] ?? "30");
            string simulationMode = "random";

            AnsiConsole.MarkupLine($"[bold]Using default configuration:[/]");
            AnsiConsole.MarkupLine($"  - [green]Users:[/] {userCount}");
            AnsiConsole.MarkupLine($"  - [green]Orders per user:[/] {ordersPerUser}");
            AnsiConsole.MarkupLine($"  - [green]Concurrency:[/] {concurrency}");
            AnsiConsole.MarkupLine($"  - [green]Timeout:[/] {timeoutSeconds} seconds");
            AnsiConsole.MarkupLine($"  - [green]Simulation mode:[/] {simulationMode}");

            AnsiConsole.WriteLine();
            if (!AnsiConsole.Confirm("Start stress test with these settings?"))
            {
                return;
            }

            Console.Clear();
            AnsiConsole.Write(new Rule("[yellow]Running Stress Test[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            // Create a single progress display to avoid concurrency issues
            var progress = AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn()
                });

            TestResult testResult = null;

            await progress.StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask("Running Stress Test", maxValue: 100);
                string testFolderPath = null;

                try
                {
                    // Create a timestamp-based folder for this test run
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    testFolderPath = Path.Combine("logs", $"stress_test_{timestamp}");
                    Directory.CreateDirectory(testFolderPath);

                    // Create a progress handler that updates our single progress bar
                    var progressHandler = new Progress<TestProgress>(p =>
                    {
                        progressTask.Value = p.Percentage;
                        if (p.Total > 0)
                        {
                            progressTask.MaxValue = p.Total;
                            progressTask.Value = p.Completed;
                            // Escape square brackets to avoid markup parsing errors
                            progressTask.Description = $"{p.Message} ({p.Completed}/{p.Total})";
                        }
                        else
                        {
                            progressTask.Description = p.Message;
                        }
                    });

                    // Update configuration
                    var configOverrides = new Dictionary<string, string>
                    {
                        ["StressTestSettings:SimulationMode"] = simulationMode,
                        ["StressTestSettings:TestFolderPath"] = testFolderPath
                    };

                    var config = new ConfigurationBuilder()
                        .AddConfiguration(_configuration)
                        .AddInMemoryCollection(configOverrides)
                        .Build();

                    // Create and run the stress test
                    var stressTestRunner = new StressTestRunner(
                        config,
                        userCount,
                        ordersPerUser,
                        concurrency,
                        timeoutSeconds,
                        progressHandler);

                    // Run the test - prevent any prompts during execution
                    testResult = await stressTestRunner.RunAsync();
                }
                catch (Exception ex)
                {
                    // Console output for error messages only, no interactive elements
                    var errorMessage = $"Error: {ex.Message}";
                    progressTask.Description = $"Test failed: {ex.Message}";

                    testResult = new TestResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now,
                        ElapsedTime = TimeSpan.Zero
                    };
                }
                finally
                {
                    progressTask.Value = 100;
                    progressTask.Description = "Test completed";
                }

                // Store the test folder path for later use
                if (testResult != null)
                {
                    testResult.TestFolderPath = testFolderPath;
                }
            });

            // Display results
            if (testResult != null)
            {
                DisplayTestResults(testResult);
                SaveReport(testResult, "stress_test", userCount, ordersPerUser, concurrency, simulationMode);
            }
        }

        private static async Task RunUnitTest()
        {
            Console.Clear();
            AnsiConsole.Write(new Rule("[yellow]Unit Test Configuration[/]").RuleStyle("grey").LeftJustified());
            AnsiConsole.WriteLine();

            // Run all test categories by default without asking for confirmation
            var testCategories = new Dictionary<string, bool>
            {
                ["Identity Service"] = true,
                ["Account Service"] = true,
                ["Market Data Service"] = true,
                ["Trading Service"] = true,
                ["Risk Service"] = true,
                ["Notification Service"] = true
            };

            var timeoutSeconds = AnsiConsole.Prompt(
                new TextPrompt<int>("Enter test timeout in seconds [green](1-300)[/]:")
                    .DefaultValue(int.Parse(_configuration["TestSettings:TestTimeout"] ?? "60"))
                    .Validate(value =>
                    {
                        return value switch
                        {
                            < 1 => ValidationResult.Error("[red]Too low! Value must be between 1 and 300.[/]"),
                            > 300 => ValidationResult.Error("[red]Too high! Value must be between 1 and 300.[/]"),
                            _ => ValidationResult.Success(),
                        };
                    }));

            AnsiConsole.WriteLine();
            if (!AnsiConsole.Confirm("Start unit tests?"))
            {
                return;
            }

            Console.Clear();
            AnsiConsole.Write(new Rule("[yellow]Running Unit Tests[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            // Configure unit test execution
            var testConfig = new Dictionary<string, string>
            {
                ["TestSettings:TestTimeout"] = timeoutSeconds.ToString(),
                ["TestSettings:RunIdentityTests"] = testCategories["Identity Service"].ToString(),
                ["TestSettings:RunAccountTests"] = testCategories["Account Service"].ToString(),
                ["TestSettings:RunMarketDataTests"] = testCategories["Market Data Service"].ToString(),
                ["TestSettings:RunTradingTests"] = testCategories["Trading Service"].ToString(),
                ["TestSettings:RunRiskTests"] = testCategories["Risk Service"].ToString(),
                ["TestSettings:RunNotificationTests"] = testCategories["Notification Service"].ToString()
            };

            var config = new ConfigurationBuilder()
                .AddConfiguration(_configuration)
                .AddInMemoryCollection(testConfig)
                .Build();

            // Create progress UI
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Initializing unit tests...", async ctx =>
                {
                    var unitTestRunner = new UnitTestRunner(config);
                    var results = await unitTestRunner.RunTestsAsync();

                    // Display results
                    DisplayUnitTestResults(results);
                    SaveUnitTestReport(results);
                });
        }

        private static void DisplayTestResults(TestResult result)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[yellow]Test Results[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            // Create a grid with two panels
            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();

            // Summary panel
            var summaryPanel = new Panel(GetSummaryContent(result))
                .Header("Summary")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            // Latency panel
            var latencyPanel = new Panel(GetLatencyContent(result))
                .Header("Latency")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            grid.AddRow(summaryPanel, latencyPanel);
            AnsiConsole.Write(grid);
            AnsiConsole.WriteLine();
        }

        private static string GetSummaryContent(TestResult result)
        {
            var successRate = result.TotalRequests > 0
                ? (double)result.SuccessfulRequests / result.TotalRequests * 100
                : 0;

            return new Markup(
                $"[bold]Success:[/] {(result.Success ? "[green]Yes[/]" : "[red]No[/]")}\n" +
                (result.Success ? "" : $"[bold]Error:[/] [red]{result.ErrorMessage}[/]\n") +
                $"[bold]Total Requests:[/] {result.TotalRequests}\n" +
                $"[bold]Successful:[/] [green]{result.SuccessfulRequests}[/]\n" +
                $"[bold]Failed:[/] [red]{result.FailedRequests}[/]\n" +
                $"[bold]Success Rate:[/] {successRate:F2}%\n" +
                $"[bold]Start Time:[/] {result.StartTime:yyyy-MM-dd HH:mm:ss}\n" +
                $"[bold]End Time:[/] {result.EndTime:yyyy-MM-dd HH:mm:ss}\n" +
                $"[bold]Duration:[/] {result.ElapsedTime.TotalSeconds:F2} seconds\n" +
                $"[bold]Orders/Second:[/] {result.OrdersPerSecond:F2}"
            ).ToString();
        }

        private static string GetLatencyContent(TestResult result)
        {
            return new Markup(
                $"[bold]Average:[/] {result.AverageLatency.TotalMilliseconds:F2} ms\n" +
                $"[bold]Min:[/] {result.MinLatency.TotalMilliseconds:F2} ms\n" +
                $"[bold]Max:[/] {result.MaxLatency.TotalMilliseconds:F2} ms\n" +
                $"[bold]Percentiles:[/]\n" +
                $"  50th: {(result.Percentiles.Count >= 1 ? result.Percentiles[0].ToString("F2") : "0")} ms\n" +
                $"  90th: {(result.Percentiles.Count >= 2 ? result.Percentiles[1].ToString("F2") : "0")} ms\n" +
                $"  95th: {(result.Percentiles.Count >= 3 ? result.Percentiles[2].ToString("F2") : "0")} ms\n" +
                $"  99th: {(result.Percentiles.Count >= 4 ? result.Percentiles[3].ToString("F2") : "0")} ms"
            ).ToString();
        }

        private static void DisplayUnitTestResults(TestRunResults results)
        {
            AnsiConsole.WriteLine();

            // Summary statistics
            int totalTests = results.Total;
            int passedTests = results.Passed;
            int failedTests = results.Failed;
            int skippedTests = results.Skipped;
            double passRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;

            // Calculate performance metrics
            var avgTestDuration = results.TestLatencies.Count > 0
                ? TimeSpan.FromTicks((long)results.TestLatencies.Average(l => l.Ticks))
                : TimeSpan.Zero;
            var minTestDuration = results.TestLatencies.Count > 0
                ? results.TestLatencies.Min()
                : TimeSpan.Zero;
            var maxTestDuration = results.TestLatencies.Count > 0
                ? results.TestLatencies.Max()
                : TimeSpan.Zero;

            // Create a grid with two panels for cleaner presentation
            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();

            // Create summary panel
            var summaryPanel = new Panel(
                new Markup(
                    $"[bold]Total Tests:[/] {totalTests}\n" +
                    $"[bold]Passed:[/] [green]{passedTests}[/]\n" +
                    $"[bold]Failed:[/] [red]{failedTests}[/]\n" +
                    $"[bold]Skipped:[/] [yellow]{skippedTests}[/]\n" +
                    $"[bold]Pass Rate:[/] [cyan]{passRate:F2}%[/]\n" +
                    $"[bold]Duration:[/] {results.Elapsed.TotalSeconds:F2} seconds"
                ).ToString())
                .Header("[blue]Test Summary[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            // Create performance panel
            var performancePanel = new Panel(
                new Markup(
                    $"[bold]Test Performance:[/]\n" +
                    $"[bold]Average:[/] {avgTestDuration.TotalMilliseconds:F2} ms\n" +
                    $"[bold]Min:[/] {minTestDuration.TotalMilliseconds:F2} ms\n" +
                    $"[bold]Max:[/] {maxTestDuration.TotalMilliseconds:F2} ms\n" +
                    $"[bold]Tests/Second:[/] {(results.Elapsed.TotalSeconds > 0 ? totalTests / results.Elapsed.TotalSeconds : 0):F2}"
                ).ToString())
                .Header("[blue]Performance Metrics[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            grid.AddRow(summaryPanel, performancePanel);
            AnsiConsole.Write(grid);
            AnsiConsole.WriteLine();

            // Create a bar chart for test result visualization
            var chart = new BarChart()
                .Width(60)
                .Label("[bold underline]Test Results Distribution[/]")
                .CenterLabel()
                .AddItem("Passed", passedTests, Color.Green)
                .AddItem("Failed", failedTests, Color.Red)
                .AddItem("Skipped", skippedTests, Color.Yellow);

            AnsiConsole.Write(chart);
            AnsiConsole.WriteLine();
        }

        private static void SaveReport(TestResult result, string testType, int userCount, int ordersPerUser, int concurrency, string simulationMode)
        {
            try
            {
                // Use the test folder path if available, otherwise create a new one
                string logDir;
                if (!string.IsNullOrEmpty(result.TestFolderPath) && Directory.Exists(result.TestFolderPath))
                {
                    logDir = result.TestFolderPath;
                }
                else
                {
                    // Create a timestamp for the filename and directory
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    // Create a specific directory for this test run
                    logDir = Path.Combine("logs", $"{testType}_{timestamp}");
                    Directory.CreateDirectory(logDir);
                }

                string filename = Path.Combine(logDir, "report.txt");

                // Write report to file
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine($"=== {testType.ToUpper()} TEST REPORT ===");
                    writer.WriteLine($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Configuration:");
                    writer.WriteLine($"  User Count: {userCount}");
                    writer.WriteLine($"  Orders Per User: {ordersPerUser}");
                    writer.WriteLine($"  Concurrency: {concurrency}");
                    writer.WriteLine($"  Simulation Mode: {simulationMode}");
                    writer.WriteLine();
                    writer.WriteLine($"Results:");
                    writer.WriteLine($"  Success: {(result.Success ? "Yes" : "No")}");
                    if (!result.Success)
                    {
                        writer.WriteLine($"  Error: {result.ErrorMessage}");
                    }
                    writer.WriteLine($"  Total Requests: {result.TotalRequests}");
                    writer.WriteLine($"  Successful Requests: {result.SuccessfulRequests}");
                    writer.WriteLine($"  Failed Requests: {result.FailedRequests}");
                    writer.WriteLine($"  Success Rate: {(result.TotalRequests > 0 ? (double)result.SuccessfulRequests / result.TotalRequests * 100 : 0):F2}%");
                    writer.WriteLine($"  Start Time: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"  End Time: {result.EndTime:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"  Duration: {result.ElapsedTime.TotalSeconds:F2} seconds");
                    writer.WriteLine($"  Orders/Second: {result.OrdersPerSecond:F2}");
                    writer.WriteLine();
                    writer.WriteLine($"Latency:");
                    writer.WriteLine($"  Average: {result.AverageLatency.TotalMilliseconds:F2} ms");
                    writer.WriteLine($"  Min: {result.MinLatency.TotalMilliseconds:F2} ms");
                    writer.WriteLine($"  Max: {result.MaxLatency.TotalMilliseconds:F2} ms");
                    writer.WriteLine($"  Percentiles:");
                    writer.WriteLine($"    50th: {(result.Percentiles.Count >= 1 ? result.Percentiles[0] : 0):F2} ms");
                    writer.WriteLine($"    90th: {(result.Percentiles.Count >= 2 ? result.Percentiles[1] : 0):F2} ms");
                    writer.WriteLine($"    95th: {(result.Percentiles.Count >= 3 ? result.Percentiles[2] : 0):F2} ms");
                    writer.WriteLine($"    99th: {(result.Percentiles.Count >= 4 ? result.Percentiles[3] : 0):F2} ms");
                }

                // Move any other related logs to this directory
                TryMoveRelatedLogs(Path.GetFileName(logDir), logDir);

                // Cleanup coverage directory
                CleanupCoverageDirectory();

                AnsiConsole.MarkupLine($"[green]Report saved to {filename}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error saving report: {ex.Message}[/]");
            }
        }

        private static void SaveUnitTestReport(TestRunResults results)
        {
            try
            {
                // Create a timestamp for the filename and directory
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Create a specific directory for this test run
                string logDir = Path.Combine("logs", $"unit_test_{timestamp}");
                Directory.CreateDirectory(logDir);

                string filename = Path.Combine(logDir, "report.txt");
                string htmlFilename = Path.Combine(logDir, "report.html");

                // Calculate statistics
                int totalTests = results.Total;
                int passedTests = results.Passed;
                int failedTests = results.Failed;
                int skippedTests = results.Skipped;
                double passRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;

                // Calculate performance metrics
                var avgTestDuration = results.TestLatencies.Count > 0
                    ? TimeSpan.FromTicks((long)results.TestLatencies.Average(l => l.Ticks))
                    : TimeSpan.Zero;
                var minTestDuration = results.TestLatencies.Count > 0
                    ? results.TestLatencies.Min()
                    : TimeSpan.Zero;
                var maxTestDuration = results.TestLatencies.Count > 0
                    ? results.TestLatencies.Max()
                    : TimeSpan.Zero;
                var testsPerSecond = results.Elapsed.TotalSeconds > 0
                    ? totalTests / results.Elapsed.TotalSeconds
                    : 0;

                // Write text report to file
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine("=== UNIT TEST REPORT ===");
                    writer.WriteLine($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();
                    writer.WriteLine("Summary:");
                    writer.WriteLine($"  Total Tests: {totalTests}");
                    writer.WriteLine($"  Passed: {passedTests}");
                    writer.WriteLine($"  Failed: {failedTests}");
                    writer.WriteLine($"  Skipped: {skippedTests}");
                    writer.WriteLine($"  Pass Rate: {passRate:F2}%");
                    writer.WriteLine($"  Duration: {results.Elapsed.TotalSeconds:F2} seconds");
                    writer.WriteLine();
                    writer.WriteLine("Performance Metrics:");
                    writer.WriteLine($"  Average Test Duration: {avgTestDuration.TotalMilliseconds:F2} ms");
                    writer.WriteLine($"  Min Test Duration: {minTestDuration.TotalMilliseconds:F2} ms");
                    writer.WriteLine($"  Max Test Duration: {maxTestDuration.TotalMilliseconds:F2} ms");
                    writer.WriteLine($"  Tests Per Second: {testsPerSecond:F2}");
                }

                // Generate HTML report with styling
                using (StreamWriter writer = new StreamWriter(htmlFilename))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html lang=\"en\">");
                    writer.WriteLine("<head>");
                    writer.WriteLine("  <meta charset=\"UTF-8\">");
                    writer.WriteLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                    writer.WriteLine("  <title>Unit Test Report</title>");
                    writer.WriteLine("  <style>");
                    writer.WriteLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; color: #333; }");
                    writer.WriteLine("    .container { max-width: 1000px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
                    writer.WriteLine("    h1 { color: #0066cc; text-align: center; margin-bottom: 30px; }");
                    writer.WriteLine("    h2 { color: #0066cc; margin-top: 30px; border-bottom: 1px solid #eee; padding-bottom: 10px; }");
                    writer.WriteLine("    .summary-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; margin-bottom: 30px; }");
                    writer.WriteLine("    .card { background-color: white; border-radius: 8px; padding: 20px; box-shadow: 0 2px 5px rgba(0,0,0,0.05); }");
                    writer.WriteLine("    .card h3 { margin-top: 0; color: #555; }");
                    writer.WriteLine("    .stat { display: flex; justify-content: space-between; margin-bottom: 10px; }");
                    writer.WriteLine("    .label { font-weight: bold; }");
                    writer.WriteLine("    .value { }");
                    writer.WriteLine("    .value.success { color: #28a745; font-weight: bold; }");
                    writer.WriteLine("    .value.warning { color: #ffc107; font-weight: bold; }");
                    writer.WriteLine("    .value.danger { color: #dc3545; font-weight: bold; }");
                    writer.WriteLine("    .value.info { color: #17a2b8; font-weight: bold; }");
                    writer.WriteLine("    .chart-container { width: 100%; height: 300px; margin-top: 20px; }");
                    writer.WriteLine("    footer { text-align: center; margin-top: 30px; font-size: 0.8em; color: #777; }");
                    writer.WriteLine("  </style>");
                    writer.WriteLine("  <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
                    writer.WriteLine("</head>");
                    writer.WriteLine("<body>");
                    writer.WriteLine("  <div class=\"container\">");
                    writer.WriteLine($"    <h1>Unit Test Report</h1>");
                    writer.WriteLine($"    <p style=\"text-align: center; color: #666;\">Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

                    // Summary Section
                    writer.WriteLine("    <h2>Test Summary</h2>");
                    writer.WriteLine("    <div class=\"summary-grid\">");

                    // Results Card
                    writer.WriteLine("      <div class=\"card\">");
                    writer.WriteLine("        <h3>Results</h3>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Total Tests:</span>");
                    writer.WriteLine($"          <span class=\"value info\">{totalTests}</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Passed:</span>");
                    writer.WriteLine($"          <span class=\"value success\">{passedTests}</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Failed:</span>");
                    writer.WriteLine($"          <span class=\"value danger\">{failedTests}</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Skipped:</span>");
                    writer.WriteLine($"          <span class=\"value warning\">{skippedTests}</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Pass Rate:</span>");
                    writer.WriteLine($"          <span class=\"value {(passRate >= 90 ? "success" : passRate >= 70 ? "warning" : "danger")}\">{passRate:F2}%</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Duration:</span>");
                    writer.WriteLine($"          <span class=\"value\">{results.Elapsed.TotalSeconds:F2} seconds</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("      </div>");

                    // Performance Card
                    writer.WriteLine("      <div class=\"card\">");
                    writer.WriteLine("        <h3>Performance Metrics</h3>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Average Duration:</span>");
                    writer.WriteLine($"          <span class=\"value\">{avgTestDuration.TotalMilliseconds:F2} ms</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Min Duration:</span>");
                    writer.WriteLine($"          <span class=\"value\">{minTestDuration.TotalMilliseconds:F2} ms</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Max Duration:</span>");
                    writer.WriteLine($"          <span class=\"value\">{maxTestDuration.TotalMilliseconds:F2} ms</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("        <div class=\"stat\">");
                    writer.WriteLine("          <span class=\"label\">Tests Per Second:</span>");
                    writer.WriteLine($"          <span class=\"value info\">{testsPerSecond:F2}</span>");
                    writer.WriteLine("        </div>");
                    writer.WriteLine("      </div>");
                    writer.WriteLine("    </div>");

                    // Charts Section
                    writer.WriteLine("    <h2>Visualization</h2>");
                    writer.WriteLine("    <div class=\"chart-container\">");
                    writer.WriteLine("      <canvas id=\"resultsChart\"></canvas>");
                    writer.WriteLine("    </div>");

                    // Footer
                    writer.WriteLine("    <footer>");
                    writer.WriteLine("      <p>Trading System Test Framework</p>");
                    writer.WriteLine("    </footer>");
                    writer.WriteLine("  </div>");

                    // JavaScript for charts
                    writer.WriteLine("  <script>");
                    writer.WriteLine("    document.addEventListener('DOMContentLoaded', function() {");
                    writer.WriteLine("      // Test Results Chart");
                    writer.WriteLine("      var ctx = document.getElementById('resultsChart').getContext('2d');");
                    writer.WriteLine("      new Chart(ctx, {");
                    writer.WriteLine("        type: 'pie',");
                    writer.WriteLine("        data: {");
                    writer.WriteLine("          labels: ['Passed', 'Failed', 'Skipped'],");
                    writer.WriteLine("          datasets: [{");
                    writer.WriteLine("            data: [" + passedTests + ", " + failedTests + ", " + skippedTests + "],");
                    writer.WriteLine("            backgroundColor: ['#28a745', '#dc3545', '#ffc107'],");
                    writer.WriteLine("            borderWidth: 1");
                    writer.WriteLine("          }]");
                    writer.WriteLine("        },");
                    writer.WriteLine("        options: {");
                    writer.WriteLine("          responsive: true,");
                    writer.WriteLine("          maintainAspectRatio: false,");
                    writer.WriteLine("          plugins: {");
                    writer.WriteLine("            legend: {");
                    writer.WriteLine("              position: 'right'");
                    writer.WriteLine("            },");
                    writer.WriteLine("            title: {");
                    writer.WriteLine("              display: true,");
                    writer.WriteLine("              text: 'Test Results'");
                    writer.WriteLine("            }");
                    writer.WriteLine("          }");
                    writer.WriteLine("        }");
                    writer.WriteLine("      });");
                    writer.WriteLine("    });");
                    writer.WriteLine("  </script>");
                    writer.WriteLine("</body>");
                    writer.WriteLine("</html>");
                }

                // Move any other related logs to this directory
                TryMoveRelatedLogs(timestamp, logDir);

                // Cleanup coverage directory
                CleanupCoverageDirectory();

                AnsiConsole.MarkupLine($"[green]Reports saved to:[/]");
                AnsiConsole.MarkupLine($"[green]- {filename}[/]");
                AnsiConsole.MarkupLine($"[green]- {htmlFilename}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error saving report: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Tries to move any log files with the given timestamp to the specified directory
        /// </summary>
        private static void TryMoveRelatedLogs(string timestamp, string targetDir)
        {
            try
            {
                // Get all log files in the logs directory that match the timestamp
                string logsDir = "logs";
                var logFiles = Directory.GetFiles(logsDir, $"*{timestamp}*.*");

                foreach (var file in logFiles)
                {
                    if (!file.Contains(Path.GetFileName(targetDir))) // Avoid moving the directory itself
                    {
                        string fileName = Path.GetFileName(file);
                        string targetFile = Path.Combine(targetDir, fileName);
                        File.Move(file, targetFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not move related log files: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the coverage directory to save disk space
        /// </summary>
        private static void CleanupCoverageDirectory()
        {
            try
            {
                string coverageDir = Path.Combine("logs", "coverage");
                if (Directory.Exists(coverageDir))
                {
                    Directory.Delete(coverageDir, true);
                    Console.WriteLine($"Cleaned up coverage directory");

                    // Recreate the empty directory for future use
                    Directory.CreateDirectory(coverageDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not clean up coverage directory: {ex.Message}");
            }
        }
    }
}