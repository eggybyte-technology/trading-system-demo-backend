using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System.IO;
using SimulationTest.Core;
using Spectre.Console;

namespace SimulationTest
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                var app = new CommandLineApplication
                {
                    Name = "SimulationTest",
                    Description = "Trading System API Test Tool"
                };

                app.HelpOption("-?|--help");

                // Add test mode option
                var modeOption = app.Option<string>(
                    "-m|--mode <MODE>",
                    "Test mode: 'unit' for unit tests, 'stress' for stress tests",
                    CommandOptionType.SingleValue);

                // Add target service option for stress tests
                var targetOption = app.Option<string>(
                    "-t|--target <SERVICE>",
                    "Target service for stress tests (identity, trading, market-data, account, risk, notification, match-making)",
                    CommandOptionType.SingleValue);

                // Add concurrency option for stress tests
                var concurrencyOption = app.Option<int>(
                    "-c|--concurrency <NUMBER>",
                    "Number of concurrent requests for stress tests",
                    CommandOptionType.SingleValue);

                // Add duration option for stress tests
                var durationOption = app.Option<int>(
                    "-d|--duration <SECONDS>",
                    "Duration of stress test in seconds",
                    CommandOptionType.SingleValue);

                // Set up configuration
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build();

                app.OnExecuteAsync(async cancellationToken =>
                {
                    var testMode = modeOption.Value() ?? "unit";

                    if (testMode.Equals("unit", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunUnitTestsAsync(config);
                    }
                    else if (testMode.Equals("stress", StringComparison.OrdinalIgnoreCase))
                    {
                        var targetService = targetOption.Value() ?? "trading";
                        var concurrency = concurrencyOption.HasValue() ? concurrencyOption.ParsedValue : 10;
                        var duration = durationOption.HasValue() ? durationOption.ParsedValue : 60;

                        await RunStressTestsAsync(config, targetService, concurrency, duration);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Invalid test mode. Use 'unit' or 'stress'.[/]");
                        return 1;
                    }

                    return 0;
                });

                return await app.ExecuteAsync(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }

        /// <summary>
        /// Runs unit tests for all services in a sequential workflow
        /// </summary>
        /// <param name="config">The application configuration</param>
        static async Task RunUnitTestsAsync(IConfiguration config)
        {
            AnsiConsole.MarkupLine("[blue]Running unit tests...[/]");

            // Use the new integrated test workflow
            var workflow = new IntegratedTestWorkflow(config);
            bool success = await workflow.RunWorkflowAsync();

            if (success)
            {
                AnsiConsole.MarkupLine("[green]Unit tests completed successfully[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Unit tests failed[/]");
            }
        }

        /// <summary>
        /// Runs stress tests against a specified service
        /// </summary>
        /// <param name="config">The application configuration</param>
        /// <param name="targetService">The service to target</param>
        /// <param name="concurrency">The number of concurrent requests</param>
        /// <param name="durationSeconds">The duration of the test in seconds</param>
        static async Task RunStressTestsAsync(IConfiguration config, string targetService, int concurrency, int durationSeconds)
        {
            AnsiConsole.MarkupLine($"[blue]Running stress tests against {targetService} service...[/]");
            AnsiConsole.MarkupLine($"[blue]Concurrency: {concurrency}, Duration: {durationSeconds} seconds[/]");

            // Create a stress test runner
            var stressTestRunner = new StressTestRunner(config, targetService, concurrency, durationSeconds);

            // Run the stress tests
            await stressTestRunner.RunAsync();

            AnsiConsole.MarkupLine("[green]Stress tests completed[/]");
        }
    }
}
