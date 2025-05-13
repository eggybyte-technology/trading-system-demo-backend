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
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Add ObjectId converter for MongoDB BSON IDs
            jsonOptions.Converters.Add(new CommonLib.Services.ObjectIdJsonConverter());

            services.AddSingleton(jsonOptions);
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

            // Create a progress display that shows both progress and status
            TestResult testResult = null;
            string testFolderPath = null;
            DateTime startTime = DateTime.Now;
            StressTestRunner testRunner = null;

            // Create a timestamp-based folder for this test run
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            testFolderPath = Path.Combine("logs", $"stress_test_{timestamp}");
            Directory.CreateDirectory(testFolderPath);

            // Setup log file path (but don't open it here)
            var logFileName = Path.Combine(testFolderPath, "test.log");

            // Run the test with a simple progress display
            await AnsiConsole.Progress()
               .AutoClear(false)
               .HideCompleted(false)
               .Columns(new ProgressColumn[]
               {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                    new ElapsedTimeColumn()
               })
               .StartAsync(async ctx =>
               {
                   // Create three tasks - the main progress task, a stats display task, and a logs display task
                   var progressTask = ctx.AddTask("[yellow]Running Stress Test[/]", maxValue: 100);
                   var statsTask = ctx.AddTask("[blue]Stats[/]", maxValue: 1);
                   var logsTask = ctx.AddTask("[green]Logs[/]", maxValue: 1);

                   // Make the stats and logs tasks display as text only without progress indicators
                   statsTask.IsIndeterminate = true;
                   statsTask.Value = 0;
                   statsTask.MaxValue = 0;

                   logsTask.IsIndeterminate = true;
                   logsTask.Value = 0;
                   logsTask.MaxValue = 0;

                   try
                   {
                       // Create a progress handler that updates our progress bar and stats display
                       var progressHandler = new Progress<TestProgress>(p =>
                       {
                           // Update progress bar
                           if (p.Total > 0)
                           {
                               progressTask.MaxValue = p.Total;
                               progressTask.Value = p.Completed;
                               progressTask.Description = $"[yellow]{p.Message} ({p.Completed}/{p.Total})[/]";
                           }
                           else
                           {
                               progressTask.Value = p.Percentage;
                               progressTask.Description = $"[yellow]{p.Message}[/]";
                           }

                           // Update stats display in the second progress task
                           if (testRunner != null)
                           {
                               var elapsedTime = DateTime.Now - startTime;
                               var completed = testRunner.GetCompletedOperations();
                               var successRate = testRunner.GetCurrentSuccessRate();
                               var latency = testRunner.GetCurrentAverageLatency();
                               var ordersPerSecond = elapsedTime.TotalSeconds > 0 ? completed / elapsedTime.TotalSeconds : 0;

                               // Update stats display with colored format
                               statsTask.Description =
                                   $"[blue]Completed:[/] [bold]{completed}[/] | " +
                                   $"[green]Success:[/] [bold]{successRate:F2}%[/] | " +
                                   $"[yellow]Latency:[/] [bold]{latency:F2} ms[/] | " +
                                   $"[cyan]Rate:[/] [bold]{ordersPerSecond:F2}/sec[/]";
                           }

                           // If there's a message, update the logs display
                           if (!string.IsNullOrEmpty(p.LogMessage))
                           {
                               var logMessage = $"[[{DateTime.Now:HH:mm:ss.fff}]] {p.LogMessage}";
                               logsTask.Description = $"[green]{logMessage}[/]";
                           }
                       });

                       // Update configuration
                       var configOverrides = new Dictionary<string, string>
                       {
                           ["StressTestSettings:SimulationMode"] = simulationMode,
                           ["StressTestSettings:TestFolderPath"] = testFolderPath,
                           ["StressTestSettings:LogFile"] = logFileName
                       };

                       var config = new ConfigurationBuilder()
                              .AddConfiguration(_configuration)
                              .AddInMemoryCollection(configOverrides)
                              .Build();

                       startTime = DateTime.Now;

                       // Create and run the stress test
                       testRunner = new StressTestRunner(
                              config,
                              userCount,
                              ordersPerUser,
                              concurrency,
                              timeoutSeconds,
                              progressHandler);

                       // Run the test
                       testResult = await testRunner.RunAsync();
                   }
                   catch (Exception ex)
                   {
                       progressTask.Description = $"[red]Test failed: {ex.Message}[/]";
                       statsTask.Description = $"[red]ERROR: {ex.Message}[/]";
                       logsTask.Description = $"[red]ERROR: {ex.Message}[/]";

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
                       progressTask.Value = progressTask.MaxValue;
                       progressTask.Description = "[green]Test completed[/]";

                       // Final stats update
                       if (testResult != null && testResult.Success)
                       {
                           statsTask.Description = $"[green]Success: {testResult.TotalRequests} orders | " +
                                  $"Success Rate: {(testResult.TotalRequests > 0 ? (double)testResult.SuccessfulRequests / testResult.TotalRequests * 100 : 0):F2}% | " +
                                  $"Avg Latency: {testResult.AverageLatency.TotalMilliseconds:F2} ms[/]";
                       }
                       else if (testResult != null)
                       {
                           statsTask.Description = $"[red]Failed: {testResult.ErrorMessage}[/]";
                       }
                   }
               });

            // Store the test folder path for later use
            if (testResult != null)
            {
                testResult.TestFolderPath = testFolderPath;
            }

            // Display results
            if (testResult != null)
            {
                DisplayTestResults(testResult);
                SaveReport(testResult, "stress_test", userCount, ordersPerUser, concurrency, simulationMode);
            }
        }

        /// <summary>
        /// Run unit tests on the system
        /// </summary>
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

            // Create a timestamp-based folder for this test run
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string testFolderPath = Path.Combine("logs", $"unit_test_{timestamp}");
            Directory.CreateDirectory(testFolderPath);

            // Setup log file path (but don't open it here)
            var logFileName = Path.Combine(testFolderPath, "test.log");

            // Configure unit test execution
            var testConfig = new Dictionary<string, string>
            {
                ["TestSettings:TestTimeout"] = timeoutSeconds.ToString(),
                ["TestSettings:RunIdentityTests"] = testCategories["Identity Service"].ToString(),
                ["TestSettings:RunAccountTests"] = testCategories["Account Service"].ToString(),
                ["TestSettings:RunMarketDataTests"] = testCategories["Market Data Service"].ToString(),
                ["TestSettings:RunTradingTests"] = testCategories["Trading Service"].ToString(),
                ["TestSettings:RunRiskTests"] = testCategories["Risk Service"].ToString(),
                ["TestSettings:RunNotificationTests"] = testCategories["Notification Service"].ToString(),
                ["TestSettings:LogFile"] = logFileName
            };

            var config = new ConfigurationBuilder()
                .AddConfiguration(_configuration)
                .AddInMemoryCollection(testConfig)
                .Build();

            TestRunResults results = null;

            try
            {
                // Create progress UI with the same pattern as stress test
                await AnsiConsole.Progress()
                    .AutoClear(false)
                    .HideCompleted(false)
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(),
                        new ElapsedTimeColumn()
                    })
                    .StartAsync(async ctx =>
                    {
                        // Create three tasks - the main progress task, a stats display task, and a logs display task
                        var progressTask = ctx.AddTask("[yellow]Running Unit Tests[/]", maxValue: 100);
                        var statsTask = ctx.AddTask("[blue]Stats[/]", maxValue: 1);
                        var logsTask = ctx.AddTask("[green]Logs[/]", maxValue: 1);

                        // Make the stats and logs tasks display as text only without progress indicators
                        statsTask.IsIndeterminate = true;
                        statsTask.Value = 0;
                        statsTask.MaxValue = 0;

                        logsTask.IsIndeterminate = true;
                        logsTask.Value = 0;
                        logsTask.MaxValue = 0;

                        try
                        {
                            // Create a progress handler
                            var progressHandler = new Progress<TestProgress>(p =>
                            {
                                // Update progress bar
                                if (p.Total > 0)
                                {
                                    progressTask.MaxValue = p.Total;
                                    progressTask.Value = p.Completed;
                                    progressTask.Description = $"[yellow]{p.Message} ({p.Completed}/{p.Total})[/]";
                                }
                                else
                                {
                                    progressTask.Value = p.Percentage;
                                    progressTask.Description = $"[yellow]{p.Message}[/]";
                                }

                                // Update stats display
                                if (p.Passed >= 0 && p.Failed >= 0)
                                {
                                    int total = p.Passed + p.Failed + (p.Skipped >= 0 ? p.Skipped : 0);
                                    double passRate = total > 0 ? (double)p.Passed / total * 100 : 0;

                                    statsTask.Description =
                                        $"[green]Passed:[/] [bold]{p.Passed}[/] | " +
                                        $"[red]Failed:[/] [bold]{p.Failed}[/] | " +
                                        $"[yellow]Skipped:[/] [bold]{p.Skipped}[/] | " +
                                        $"[blue]Pass Rate:[/] [bold]{passRate:F2}%[/]";
                                }

                                // If there's a message, update the logs display
                                if (!string.IsNullOrEmpty(p.LogMessage))
                                {
                                    var logMessage = $"[[{DateTime.Now:HH:mm:ss.fff}]] {p.LogMessage}";
                                    logsTask.Description = $"[green]{logMessage}[/]";
                                }
                            });

                            var unitTestRunner = new UnitTestRunner(config, progressHandler);
                            results = await unitTestRunner.RunTestsAsync();
                        }
                        catch (Exception ex)
                        {
                            progressTask.Description = $"[red]Test failed: {ex.Message}[/]";
                            statsTask.Description = $"[red]ERROR: {ex.Message}[/]";
                            logsTask.Description = $"[red]ERROR: {ex.Message}[/]";
                        }
                        finally
                        {
                            progressTask.Value = progressTask.MaxValue;
                            progressTask.Description = "[green]Tests completed[/]";
                        }
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            // Display results
            if (results != null)
            {
                DisplayUnitTestResults(results);
                SaveUnitTestReport(results, testFolderPath);
            }
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

            return $"[bold]Success:[/] {(result.Success ? "[green]Yes[/]" : "[red]No[/]")}\n" +
                (result.Success ? "" : $"[bold]Error:[/] [red]{result.ErrorMessage}[/]\n") +
                $"[bold]Total Requests:[/] {result.TotalRequests}\n" +
                $"[bold]Successful:[/] [green]{result.SuccessfulRequests}[/]\n" +
                $"[bold]Failed:[/] [red]{result.FailedRequests}[/]\n" +
                $"[bold]Success Rate:[/] {successRate:F2}%\n" +
                $"[bold]Start Time:[/] {result.StartTime:yyyy-MM-dd HH:mm:ss}\n" +
                $"[bold]End Time:[/] {result.EndTime:yyyy-MM-dd HH:mm:ss}\n" +
                $"[bold]Duration:[/] {result.ElapsedTime.TotalSeconds:F2} seconds\n" +
                $"[bold]Orders/Second:[/] {result.OrdersPerSecond:F2}";
        }

        private static string GetLatencyContent(TestResult result)
        {
            return $"[bold]Average:[/] {result.AverageLatency.TotalMilliseconds:F2} ms\n" +
                $"[bold]Min:[/] {result.MinLatency.TotalMilliseconds:F2} ms\n" +
                $"[bold]Max:[/] {result.MaxLatency.TotalMilliseconds:F2} ms\n" +
                $"[bold]Percentiles:[/]\n" +
                $"  50th: {(result.Percentiles.Count >= 1 ? result.Percentiles[0].ToString("F2") : "0")} ms\n" +
                $"  90th: {(result.Percentiles.Count >= 2 ? result.Percentiles[1].ToString("F2") : "0")} ms\n" +
                $"  95th: {(result.Percentiles.Count >= 3 ? result.Percentiles[2].ToString("F2") : "0")} ms\n" +
                $"  99th: {(result.Percentiles.Count >= 4 ? result.Percentiles[3].ToString("F2") : "0")} ms";
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
                ))
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
                ))
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

                // Calculate success rate for easy access
                double successRate = result.TotalRequests > 0
                    ? (double)result.SuccessfulRequests / result.TotalRequests * 100
                    : 0;

                // Write text-only report to file
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine($"=== {testType.ToUpper()} TEST REPORT ===");
                    writer.WriteLine($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();
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
                    writer.WriteLine($"  Success Rate: {successRate:F2}%");
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

                AnsiConsole.MarkupLine($"[green]Report saved to:[/]");
                AnsiConsole.MarkupLine($"[green]- {filename}[/]");
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

        private static void SaveUnitTestReport(TestRunResults results, string testFolderPath = null)
        {
            try
            {
                // Use the provided test folder path if available, otherwise create a new one
                string logDir;
                if (!string.IsNullOrEmpty(testFolderPath) && Directory.Exists(testFolderPath))
                {
                    logDir = testFolderPath;
                }
                else
                {
                    // Create a timestamp for the filename and directory
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    // Create a specific directory for this test run
                    logDir = Path.Combine("logs", $"unit_test_{timestamp}");
                    Directory.CreateDirectory(logDir);
                }

                string filename = Path.Combine(logDir, "report.txt");

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

                // Move any other related logs to this directory
                TryMoveRelatedLogs(Path.GetFileName(logDir), logDir);

                // Cleanup coverage directory
                CleanupCoverageDirectory();

                AnsiConsole.MarkupLine($"[green]Report saved to:[/]");
                AnsiConsole.MarkupLine($"[green]- {filename}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error saving report: {ex.Message}[/]");
            }
        }
    }
}