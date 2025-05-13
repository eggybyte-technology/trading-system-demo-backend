using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CommonLib.Api;
using SimulationTest.Tests;
using SimulationTest.Core;
using System.Reflection;
using Spectre.Console;

namespace SimulationTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Create test logger
            var testLogger = TestLogger.Instance;

            // Create ILogger adapter for CommonLib services
            var loggerAdapter = new TestLoggerAdapter(testLogger);

            // Setup DI
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddReducedTradingSystemServices(loggerAdapter) // 使用自定义方法，移除RiskService和NotificationService
                .AddSingleton(testLogger)  // Use the singleton instance
                .AddSingleton<ReportGenerator>()
                .AddTransient<StressTest>()
                .AddTransient<UnitTest>()
                .BuildServiceProvider();

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Welcome message
            AnsiConsole.Clear();

            var rule = new Rule("[yellow]TRADING SYSTEM SIMULATION TESTS[/]");
            rule.Style = Style.Parse("yellow dim");
            // rule.Alignment = Justify.Center;

            AnsiConsole.Write(rule);

            var table = new Table();
            table.Border(TableBorder.None);
            table.AddColumn(new TableColumn("Info").Centered());

            var version = Assembly.GetExecutingAssembly().GetName().Version;

            table.AddRow($"[blue]Version:[/] [green]{version?.ToString() ?? "1.0.0"}[/]");
            table.AddRow($"[blue]Time:[/] [green]{DateTime.Now:yyyy-MM-dd HH:mm:ss}[/]");
            table.AddRow($"[blue]Process ID:[/] [green]{Environment.ProcessId}[/]");
            table.Alignment(Justify.Center);

            AnsiConsole.Write(table);

            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Please select a test to run:")
                        .PageSize(10)
                        .HighlightStyle(new Style(foreground: Color.Green))
                        .AddChoices(new[] {
                            "1. Stress Test",
                            "2. Unit Test",
                            "0. Exit"
                        }));

                switch (choice)
                {
                    case "1. Stress Test":
                        await RunStressTest(serviceProvider);
                        break;
                    case "2. Unit Test":
                        await RunUnitTest(serviceProvider);
                        break;
                    case "0. Exit":
                        AnsiConsole.MarkupLine("[yellow]Exiting...[/]");
                        return;
                    default:
                        AnsiConsole.MarkupLine("[red]Invalid choice. Please try again.[/]");
                        break;
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[blue]Press any key to return to the main menu...[/]");
                Console.ReadKey();
                AnsiConsole.Clear();
            }
        }

        private static async Task RunStressTest(ServiceProvider serviceProvider)
        {
            AnsiConsole.Clear();

            var title = new FigletText("Stress Test")
                .LeftJustified()
                .Color(Color.Yellow);

            AnsiConsole.Write(title);
            AnsiConsole.WriteLine();

            // Get configuration for stress test
            var userCount = AnsiConsole.Prompt(
                new TextPrompt<int>("Number of users to register [green](default: 10)[/] [yellow](max: 100)[/]:")
                    .DefaultValue(10)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(count =>
                    {
                        if (count <= 0)
                            return ValidationResult.Error("[red]User count must be greater than 0[/]");
                        if (count > 100)
                            return ValidationResult.Error("[red]User count cannot exceed 100[/]");
                        return ValidationResult.Success();
                    })
                    .ShowDefaultValue());

            var orderCount = AnsiConsole.Prompt(
                new TextPrompt<int>("Number of orders per user [green](default: 1000)[/] [yellow](max: 10000)[/]:")
                    .DefaultValue(1000)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(count =>
                    {
                        if (count <= 0)
                            return ValidationResult.Error("[red]Order count must be greater than 0[/]");
                        if (count > 10000)
                            return ValidationResult.Error("[red]Order count cannot exceed 10000[/]");
                        return ValidationResult.Success();
                    })
                    .ShowDefaultValue());

            AnsiConsole.WriteLine();

            // Display info panel
            var infoPanel = new Panel(new Markup($"[yellow]Starting stress test with:[/]\n[green]- {userCount}[/] users\n[green]- {orderCount}[/] orders per user\n[green]- {userCount}[/] parallel tasks (one per user)"));
            infoPanel.Border = BoxBorder.Rounded;
            infoPanel.Padding = new Padding(1, 0, 1, 0);

            AnsiConsole.Write(infoPanel);
            AnsiConsole.WriteLine();

            // Get the stress test service and run the test
            var stressTest = serviceProvider.GetRequiredService<StressTest>();
            await stressTest.RunAsync(userCount, orderCount);
        }

        private static async Task RunUnitTest(ServiceProvider serviceProvider)
        {
            AnsiConsole.Clear();

            var title = new FigletText("Unit Test")
                .LeftJustified()
                .Color(Color.Yellow);

            AnsiConsole.Write(title);
            AnsiConsole.WriteLine();

            var infoPanel = new Panel(new Markup("[yellow]Starting unit test[/]\nTesting all service methods in sequence"));
            infoPanel.Border = BoxBorder.Rounded;
            infoPanel.Padding = new Padding(1, 0, 1, 0);

            AnsiConsole.Write(infoPanel);
            AnsiConsole.WriteLine();

            // Get the unit test service and run the test
            var unitTest = serviceProvider.GetRequiredService<UnitTest>();
            await unitTest.RunAsync();
        }
    }

    /// <summary>
    /// Extension methods for registering required API service clients for the simulation tests
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registers only the required trading system API clients with the service collection and a custom logger
        /// 移除RiskService和NotificationService
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="logger">The logger to use for all services</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddReducedTradingSystemServices(this IServiceCollection services, ILogger logger)
        {
            services.AddHttpClient();

            // Register needed services with the custom logger
            services.AddScoped(sp => new IdentityService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new AccountService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new MarketDataService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new TradingService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            services.AddScoped(sp => new MatchMakingService(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                logger));

            return services;
        }
    }

    /// <summary>
    /// Adapter class to bridge TestLogger to ILogger interface for CommonLib services
    /// </summary>
    public class TestLoggerAdapter : ILogger
    {
        private readonly TestLogger _testLogger;

        public TestLoggerAdapter(TestLogger testLogger)
        {
            _testLogger = testLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _testLogger.Debug(message);
                    break;
                case LogLevel.Information:
                    _testLogger.Info(message);
                    break;
                case LogLevel.Warning:
                    _testLogger.Warning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    _testLogger.Error(message);
                    break;
                default:
                    _testLogger.Info(message);
                    break;
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            private NullScope() { }

            public void Dispose() { }
        }
    }
}
