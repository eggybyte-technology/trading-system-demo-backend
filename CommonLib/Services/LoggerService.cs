using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Services
{
    /// <summary>
    /// Interface for logger service
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// Logs a trace message
        /// </summary>
        void LogTrace(string message);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        void LogDebug(string message);

        /// <summary>
        /// Logs an information message
        /// </summary>
        void LogInformation(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message
        /// </summary>
        void LogError(string message, Exception? exception = null);

        /// <summary>
        /// Logs a critical message
        /// </summary>
        void LogCritical(string message, Exception? exception = null);
    }

    /// <summary>
    /// Implementation of the logger service
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        /// <summary>
        /// Initializes a new instance of the logger service
        /// </summary>
        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void LogTrace(string message)
        {
            _logger.LogTrace(message);
        }

        /// <inheritdoc/>
        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }

        /// <inheritdoc/>
        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        /// <inheritdoc/>
        public void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger.LogError(exception, message);
            }
            else
            {
                _logger.LogError(message);
            }
        }

        /// <inheritdoc/>
        public void LogCritical(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger.LogCritical(exception, message);
            }
            else
            {
                _logger.LogCritical(message);
            }
        }
    }

    /// <summary>
    /// Extension methods for registering and using the LoggerService
    /// </summary>
    public static class LoggerServiceExtensions
    {
        /// <summary>
        /// Adds the LoggerService to the service collection
        /// </summary>
        public static IServiceCollection AddLoggerService(this IServiceCollection services)
        {
            services.AddScoped<ILoggerService, LoggerService>();
            return services;
        }
    }
}