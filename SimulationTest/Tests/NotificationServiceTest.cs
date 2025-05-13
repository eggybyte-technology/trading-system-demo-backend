using System.Diagnostics;
using CommonLib.Models;
using CommonLib.Models.Notification;
using SimulationTest.Core;
using CommonLib.Api;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Notification Service
    /// </summary>
    public class NotificationServiceTest
    {
        private readonly CommonLib.Api.NotificationService _notificationService;
        private readonly TestLogger _logger;
        private readonly StatusBar _statusBar;
        private readonly List<OperationResult> _results = new();
        private readonly TestContext _context;

        public NotificationServiceTest(
            CommonLib.Api.NotificationService notificationService,
            TestLogger logger,
            StatusBar statusBar,
            TestContext context)
        {
            _notificationService = notificationService;
            _logger = logger;
            _statusBar = statusBar;
            _context = context;
        }

        /// <summary>
        /// Get notification settings
        /// </summary>
        public async Task<NotificationSettingsResponse> TestGetNotificationSettingsAsync()
        {
            string operationType = "NotificationService.GetNotificationSettingsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _notificationService.GetNotificationSettingsAsync(_context.Token);

                // Verify response
                if (result == null)
                    throw new AssertionException("Notification settings response should not be null");
                if (result.UserId != _context.UserId)
                    throw new AssertionException($"User ID should match. Expected: {_context.UserId}, Got: {result.UserId}");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        public async Task<NotificationSettingsResponse> TestUpdateNotificationSettingsAsync()
        {
            string operationType = "NotificationService.UpdateNotificationSettingsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var updateRequest = new NotificationSettingsUpdateRequest
                {
                    EmailEnabled = true,
                    PushEnabled = true,
                    TypeSettings = new Dictionary<string, bool>
                    {
                        { "order", true },
                        { "trade", true },
                        { "system", true }
                    }
                };

                var result = await _notificationService.UpdateNotificationSettingsAsync(_context.Token, updateRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Updated notification settings response should not be null");
                if (result.UserId != _context.UserId)
                    throw new AssertionException($"User ID should match. Expected: {_context.UserId}, Got: {result.UserId}");
                if (!result.EmailNotifications)
                    throw new AssertionException("Email notifications should be enabled");
                if (!result.PushNotifications)
                    throw new AssertionException("Push notifications should be enabled");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get notifications
        /// </summary>
        public async Task<PaginatedResult<NotificationResponse>> TestGetNotificationsAsync()
        {
            string operationType = "NotificationService.GetNotificationsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryRequest = new NotificationQueryRequest
                {
                    Page = 1,
                    PageSize = 10,
                    IncludeRead = true
                };

                var result = await _notificationService.GetNotificationsAsync(_context.Token, queryRequest);

                // Store a notification ID if available for later use
                if (result.Items != null && result.Items.Any())
                {
                    _context.NotificationId = result.Items.First().Id;
                }

                // Verify response
                if (result == null)
                    throw new AssertionException("Notifications response should not be null");
                if (result.Items == null)
                    throw new AssertionException("Notification items should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public async Task<NotificationResponse> TestMarkNotificationAsReadAsync()
        {
            string operationType = "NotificationService.MarkNotificationAsReadAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _notificationService.MarkNotificationAsReadAsync(_context.Token, _context.NotificationId);

                // Verify response
                if (result == null)
                    throw new AssertionException("Mark notification response should not be null");
                if (result.Id != _context.NotificationId)
                    throw new AssertionException($"Notification ID should match. Expected: {_context.NotificationId}, Got: {result.Id}");
                if (!result.IsRead)
                    throw new AssertionException("Notification should be marked as read");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        private void ReportSuccess(string operationType, long latencyMs)
        {
            _results.Add(new OperationResult
            {
                OperationType = operationType,
                UserId = _context.UserId,
                Success = true,
                LatencyMs = latencyMs,
                Timestamp = DateTime.Now
            });

            _statusBar.ReportSuccess(latencyMs);
        }

        private void ReportFailure(string operationType, string errorMessage, long latencyMs)
        {
            _results.Add(new OperationResult
            {
                OperationType = operationType,
                UserId = _context.UserId,
                Success = false,
                LatencyMs = latencyMs,
                Timestamp = DateTime.Now,
                ErrorMessage = errorMessage
            });

            _statusBar.ReportFailure();
        }

        public List<OperationResult> GetResults() => _results;
    }
}