using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace SimulationTest.Core
{
    /// <summary>
    /// Provides a unified way to display test progress for both unit tests and stress tests
    /// </summary>
    public class TestProgressDisplay : IDisposable
    {
        private ProgressTask _progressTask;
        private ProgressTask _statsTask;
        private ProgressTask _logsTask;
        private IProgress<TestProgress> _progressReporter;
        private TestType _testType;
        private bool _disposed = false;

        /// <summary>
        /// Gets the progress reporter that can be passed to test runners
        /// </summary>
        public IProgress<TestProgress> ProgressReporter => _progressReporter;

        /// <summary>
        /// Enum representing the type of test being displayed
        /// </summary>
        public enum TestType
        {
            UnitTest,
            StressTest
        }

        /// <summary>
        /// Initializes a new instance of the TestProgressDisplay class
        /// </summary>
        /// <param name="testType">The type of test being displayed</param>
        public TestProgressDisplay(TestType testType)
        {
            _testType = testType;
            _progressReporter = new Progress<TestProgress>(UpdateProgress);
        }

        /// <summary>
        /// Starts the progress display
        /// </summary>
        /// <returns>A task that completes when the progress display is shown</returns>
        public async Task<bool> StartAsync(Func<IProgress<TestProgress>, Task> testAction)
        {
            bool success = false;

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
                    // 以三行方式创建任务：日志行、进度行和状态行
                    // 只有第二行（进度行）显示进度条、百分比和时间
                    string title = _testType == TestType.UnitTest ?
                        "[yellow]Running Unit Tests[/]" :
                        "[yellow]Running Stress Test[/]";

                    // 日志行 - 只显示文本信息，不显示进度条等
                    _logsTask = ctx.AddTask("[green]Logs[/]");
                    _logsTask.IsIndeterminate = true;
                    _logsTask.Value = 0;
                    _logsTask.MaxValue = 0;
                    _logsTask.Description = "[green]Initializing...[/]";

                    // 进度行 - 显示进度条、百分比和时间
                    _progressTask = ctx.AddTask(title, maxValue: 100);

                    // 状态行 - 只显示文本统计信息，不显示进度条等
                    _statsTask = ctx.AddTask("[blue]Stats[/]");
                    _statsTask.IsIndeterminate = true;
                    _statsTask.Value = 0;
                    _statsTask.MaxValue = 0;
                    _statsTask.Description = "[blue]Preparing test data...[/]";

                    try
                    {
                        // Run the test action with our progress reporter
                        await testAction(_progressReporter);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        _logsTask.Description = $"[red]ERROR: {ex.Message}[/]";
                        _progressTask.Description = $"[red]Test failed[/]";
                        _statsTask.Description = $"[red]ERROR: {ex.Message}[/]";
                    }
                    finally
                    {
                        _progressTask.Value = _progressTask.MaxValue;
                        _progressTask.Description = "[green]Test completed[/]";
                    }
                });

            return success;
        }

        /// <summary>
        /// Updates the progress display with new progress information
        /// </summary>
        /// <param name="progress">The progress information</param>
        private void UpdateProgress(TestProgress progress)
        {
            // 更新第一行 - 日志信息
            if (!string.IsNullOrEmpty(progress.LogMessage))
            {
                var logMessage = $"[[{progress.Timestamp:HH:mm:ss.fff}]] {progress.LogMessage}";
                _logsTask.Description = $"[green]{logMessage}[/]";
            }

            // 更新第二行 - 进度条、百分比和时间
            if (progress.Total > 0)
            {
                _progressTask.MaxValue = progress.Total;
                _progressTask.Value = progress.Completed;
                _progressTask.Description = $"[yellow]{progress.Message} ({progress.Completed}/{progress.Total})[/]";
            }
            else
            {
                _progressTask.Value = progress.Percentage;
                _progressTask.Description = $"[yellow]{progress.Message}[/]";
            }

            // 更新第三行 - 状态信息
            if (_testType == TestType.UnitTest && progress.Passed >= 0 && progress.Failed >= 0)
            {
                int total = progress.Passed + progress.Failed + (progress.Skipped >= 0 ? progress.Skipped : 0);
                double passRate = total > 0 ? (double)progress.Passed / total * 100 : 0;

                _statsTask.Description =
                    $"[green]Passed:[/] [bold]{progress.Passed}[/] | " +
                    $"[red]Failed:[/] [bold]{progress.Failed}[/] | " +
                    $"[yellow]Skipped:[/] [bold]{progress.Skipped}[/] | " +
                    $"[blue]Pass Rate:[/] [bold]{passRate:F2}%[/]";
            }
            else if (_testType == TestType.StressTest && progress.AverageLatency >= 0 && progress.SuccessRate >= 0)
            {
                _statsTask.Description =
                    $"[blue]Completed:[/] [bold]{progress.Completed}[/] | " +
                    $"[green]Success:[/] [bold]{progress.SuccessRate:F2}%[/] | " +
                    $"[yellow]Latency:[/] [bold]{progress.AverageLatency:F2} ms[/] | " +
                    $"[cyan]Rate:[/] [bold]{progress.OperationsPerSecond:F2}/sec[/]";
            }

            // 如果是最终更新，设置进度为最大值
            if (progress.IsFinal)
            {
                _progressTask.Value = _progressTask.MaxValue;
                _progressTask.Description = "[green]Test completed[/]";
            }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Nothing to dispose for now, but this pattern allows for
                // future addition of disposable resources
                _disposed = true;
            }
        }
    }
}