using System.Collections.Concurrent;
using System.Text;
using System.IO;
using Spectre.Console;

namespace SimulationTest.Core
{
    /// <summary>
    /// Logger for test execution that handles both console output and file logging
    /// with singleton pattern to ensure a single instance handles file operations
    /// </summary>
    public class TestLogger
    {
        private static readonly Lazy<TestLogger> _instance = new Lazy<TestLogger>(() => new TestLogger());

        /// <summary>
        /// Gets the singleton instance of the logger
        /// </summary>
        public static TestLogger Instance => _instance.Value;

        private readonly ConcurrentQueue<LogEntry> _logEntries = new();
        private string _logFilePath;
        private readonly object _fileLock = new();
        private readonly object _consoleLock = new();
        private readonly string _processId = Guid.NewGuid().ToString();

        // Private constructor for singleton pattern
        private TestLogger() { }

        /// <summary>
        /// Create a new log directory for a test run
        /// </summary>
        /// <param name="testType">Type of the test (Stress or Unit)</param>
        /// <returns>Path to the created directory</returns>
        public string CreateLogDirectory(string testType)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dirPath = Path.Combine("logs", $"{testType.ToLower()}_test_{timestamp}_{_processId}");
            Directory.CreateDirectory(dirPath);

            _logFilePath = Path.Combine(dirPath, "test_log.txt");

            lock (_fileLock)
            {
                using var file = File.CreateText(_logFilePath);
                file.WriteLine($"=== {testType} Test Log - Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Process ID: {_processId} ===");
                file.WriteLine();
            }

            // Use Spectre.Console for richer display
            var panel = new Panel($"Log directory created at: [green]{dirPath}[/]")
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0, 1, 0)
            };
            panel.BorderStyle = new Style(foreground: Color.Blue);

            AnsiConsole.Write(panel);

            // Still log the message to the file without formatting
            LogMessage(LogLevel.Info, $"Log directory created at: {dirPath}", logToConsole: false);

            return dirPath;
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public void Info(string message)
        {
            LogMessage(LogLevel.Info, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void Warning(string message)
        {
            LogMessage(LogLevel.Warning, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void Error(string message)
        {
            LogMessage(LogLevel.Error, message);
        }

        /// <summary>
        /// Log a success message
        /// </summary>
        public void Success(string message)
        {
            LogMessage(LogLevel.Success, message);
        }

        /// <summary>
        /// Log a debug message (only goes to file, not console)
        /// </summary>
        public void Debug(string message)
        {
            LogMessage(LogLevel.Debug, message, logToConsole: false);
        }

        /// <summary>
        /// Log HTTP request details for debugging
        /// </summary>
        public void LogHttpRequest(HttpRequestMessage request, string serviceType)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[HTTP Request] - {serviceType}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"URL: {request.RequestUri}");

            if (request.Headers != null && request.Headers.Any())
            {
                sb.AppendLine("Headers:");
                foreach (var header in request.Headers)
                {
                    string headerValue = header.Key == "Authorization"
                        ? "Bearer [token-hidden]"
                        : string.Join(", ", header.Value);
                    sb.AppendLine($"  {header.Key}: {headerValue}");
                }
            }

            // Log request body if it exists
            if (request.Content != null)
            {
                try
                {
                    string content = request.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(content))
                    {
                        // Try to format if it's JSON
                        if (request.Content.Headers.ContentType?.MediaType == "application/json")
                        {
                            try
                            {
                                var jsonObj = System.Text.Json.JsonSerializer.Deserialize<object>(content);
                                content = System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            }
                            catch
                            {
                                // Keep original content if JSON parsing fails
                            }
                        }

                        // Limit content length for large bodies
                        const int maxContentLength = 4000;
                        string truncatedContent = content.Length > maxContentLength
                            ? content.Substring(0, maxContentLength) + "... [truncated]"
                            : content;

                        sb.AppendLine("Body:");
                        sb.AppendLine(truncatedContent);
                    }
                    else
                    {
                        sb.AppendLine("Body: (empty)");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Body: (Error reading body: {ex.Message})");
                }
            }
            else
            {
                sb.AppendLine("Body: (none)");
            }

            Debug(sb.ToString());
        }

        /// <summary>
        /// Log HTTP response details for debugging
        /// </summary>
        public void LogHttpResponse(HttpResponseMessage response, string content, long elapsedMs, string serviceType)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[HTTP Response] - {serviceType} - {elapsedMs}ms");
            sb.AppendLine($"Status: {(int)response.StatusCode} {response.StatusCode}");

            if (response.Headers != null && response.Headers.Any())
            {
                sb.AppendLine("Headers:");
                foreach (var header in response.Headers)
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            // Limit response content to avoid excessive logging
            const int maxContentLength = 4000;
            string truncatedContent = content.Length > maxContentLength
                ? content.Substring(0, maxContentLength) + "... [truncated]"
                : content;

            sb.AppendLine("Content:");
            sb.AppendLine(truncatedContent);

            Debug(sb.ToString());
        }

        private void LogMessage(LogLevel level, string message, bool logToConsole = true)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                ProcessId = _processId
            };

            _logEntries.Enqueue(entry);

            // Write to file
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    lock (_fileLock)
                    {
                        File.AppendAllText(_logFilePath, FormatLogEntry(entry) + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    // Just skip file writing if it fails
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }

            // Write to console if specified
            if (logToConsole && level != LogLevel.Debug)
            {
                lock (_consoleLock)
                {
                    WriteSpectreLogToConsole(entry);
                }
            }
        }

        /// <summary>
        /// Formats a log entry for output
        /// </summary>
        private string FormatLogEntry(LogEntry entry)
        {
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.ProcessId}] {entry.Message}";
        }

        /// <summary>
        /// Writes a colored log entry to the console using Spectre.Console
        /// </summary>
        private void WriteSpectreLogToConsole(LogEntry entry)
        {
            var timestamp = $"[grey][[{entry.Timestamp:HH:mm:ss.fff}]][/]";
            var level = GetSpectreLogLevelText(entry.Level);
            var message = entry.Message;

            // If message contains newlines, indent them properly
            if (message.Contains('\n'))
            {
                var lines = message.Split('\n');
                message = string.Join("\n   ", lines);
            }

            AnsiConsole.MarkupLine($"{timestamp} {level} {message}");
        }

        /// <summary>
        /// Gets the colored log level text for Spectre.Console
        /// </summary>
        private string GetSpectreLogLevelText(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => "[blue][[INFO]][/]",
                LogLevel.Warning => "[yellow][[WARN]][/]",
                LogLevel.Error => "[red][[ERROR]][/]",
                LogLevel.Success => "[green][[SUCCESS]][/]",
                LogLevel.Debug => "[grey][[DEBUG]][/]",
                _ => "[blue][[INFO]][/]"
            };
        }

        /// <summary>
        /// Gets the console color for a log level
        /// </summary>
        private ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => ConsoleColor.Cyan,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Success => ConsoleColor.Green,
                LogLevel.Debug => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Gets all log entries
        /// </summary>
        public List<LogEntry> GetAllLogs()
        {
            return _logEntries.ToList();
        }

        /// <summary>
        /// Entry in the log
        /// </summary>
        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public string ProcessId { get; set; }
        }

        /// <summary>
        /// Log level
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success,
            Debug
        }
    }
}