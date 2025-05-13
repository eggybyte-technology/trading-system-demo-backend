using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CommonLib.Api;
using SimulationTest.Tests;
using SimulationTest.Core;
using System.Reflection;

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
                .AddTradingSystemServices(loggerAdapter) // Use the extension method with our logger
                .AddSingleton(testLogger)  // Use the singleton instance
                .AddSingleton<ReportGenerator>()
                .AddTransient<StressTest>()
                .AddTransient<UnitTest>()
                .BuildServiceProvider();

            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");

            // Welcome message
            Console.Clear();
            Console.WriteLine("=======================================================");
            Console.WriteLine("||          TRADING SYSTEM SIMULATION TESTS          ||");
            Console.WriteLine("=======================================================");
            Console.WriteLine("Version: 1.0.0");
            Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Process ID: {Environment.ProcessId}");
            Console.WriteLine("=======================================================");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Please select a test to run:");
                Console.WriteLine("1. Stress Test");
                Console.WriteLine("2. Unit Test");
                Console.WriteLine("0. Exit");
                Console.Write("\nYour choice: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await RunStressTest(serviceProvider);
                        break;
                    case "2":
                        await RunUnitTest(serviceProvider);
                        break;
                    case "0":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }

                Console.WriteLine("\nPress any key to return to the main menu...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static async Task RunStressTest(ServiceProvider serviceProvider)
        {
            Console.Clear();
            Console.WriteLine("=======================================================");
            Console.WriteLine("||                   STRESS TEST                     ||");
            Console.WriteLine("=======================================================");
            Console.WriteLine();

            // Get configuration for stress test
            Console.Write("Number of users to register (default: 10): ");
            var userCountInput = Console.ReadLine();
            int userCount = string.IsNullOrWhiteSpace(userCountInput) ? 10 : int.Parse(userCountInput);

            Console.Write("Number of orders per user (default: 1000): ");
            var orderCountInput = Console.ReadLine();
            int orderCount = string.IsNullOrWhiteSpace(orderCountInput) ? 1000 : int.Parse(orderCountInput);

            Console.WriteLine("\nStarting stress test...");
            Console.WriteLine($"Registering {userCount} users and creating {orderCount} orders per user");
            Console.WriteLine();

            // Get the stress test service and run the test
            var stressTest = serviceProvider.GetRequiredService<StressTest>();
            await stressTest.RunAsync(userCount, orderCount);
        }

        private static async Task RunUnitTest(ServiceProvider serviceProvider)
        {
            Console.Clear();
            Console.WriteLine("=======================================================");
            Console.WriteLine("||                   UNIT TEST                       ||");
            Console.WriteLine("=======================================================");
            Console.WriteLine();

            Console.WriteLine("Starting unit test...");
            Console.WriteLine("Testing all service methods in sequence");
            Console.WriteLine();

            // Get the unit test service and run the test
            var unitTest = serviceProvider.GetRequiredService<UnitTest>();
            await unitTest.RunAsync();
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
