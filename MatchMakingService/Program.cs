using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommonLib.Services;
using MatchMakingService.Services;
using MatchMakingService.Repositories;
using System.Text.Json;

namespace MatchMakingService
{
    /// <summary>
    /// Main program class for the MatchMakingService
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the application
        /// </summary>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates the host builder
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Configure JSON serialization to handle ObjectId
                    services.AddControllers()
                        .AddJsonOptions(options =>
                        {
                            options.JsonSerializerOptions.Converters.Add(new ObjectIdJsonConverter());
                        });

                    // Add MongoDB connection
                    services.AddSingleton<MongoDbConnectionFactory>();

                    // Add logger service
                    services.AddSingleton<ILoggerService, LoggerService>();

                    // Add repositories
                    services.AddSingleton<OrderMatcherRepository>();
                    services.AddSingleton<MatchingJobRepository>();
                    services.AddSingleton<OrderRepository>();

                    // Add custom services for order matching
                    services.AddSingleton<OrderMatchingService>();

                    // Register the hosted service that runs the matching jobs
                    services.AddHostedService<OrderMatchingBackgroundService>();

                    // Register HttpClientService for inter-service communication
                    services.AddHttpClient();
                    services.AddScoped<IHttpClientService, HttpClientService>();
                });
    }
}
