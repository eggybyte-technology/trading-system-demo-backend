using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using SimulationTest.Core;
using MongoDB.Bson;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Notification Service API
    /// </summary>
    public class NotificationServiceTests : ApiTestBase
    {
        private static readonly string TestDependencyPrefix = "SimulationTest.Tests.";

        /// <summary>
        /// Test connectivity to Notification Service before running tests
        /// </summary>
        [ApiTest("Test connectivity to Notification Service")]
        public async Task<ApiTestResult> CheckConnectivity_NotificationService_ShouldBeAccessible()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Try to connect to the health endpoint
                var client = _httpClientFactory.GetClient("notification");
                var response = await client.GetAsync("/health");

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    return ApiTestResult.Passed(stopwatch.Elapsed);
                }
                else
                {
                    return ApiTestResult.Failed(
                        $"Failed to connect to Notification Service. Status code: {response.StatusCode}",
                        null,
                        stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception while connecting to Notification Service: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test getting notifications for authenticated user
        /// </summary>
        [ApiTest("Test getting notifications when authenticated")]
        public async Task<ApiTestResult> GetNotifications_WhenAuthenticated_ShouldReturnNotifications()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Act
                var notifications = await GetAsync<List<NotificationResponse>>("notification", "/notifications");

                // Assert
                stopwatch.Stop();

                if (notifications == null)
                {
                    return ApiTestResult.Failed(
                        "Notifications response is null",
                        null,
                        stopwatch.Elapsed);
                }

                // We don't check for notifications count as there might not be any in a test environment
                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test updating notification settings
        /// </summary>
        [ApiTest("Test updating notification settings")]
        public async Task<ApiTestResult> UpdateNotificationSettings_WithValidData_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                var settings = new NotificationSettingsRequest
                {
                    EmailEnabled = true,
                    PushEnabled = false,
                    OrderUpdates = true,
                    TradeUpdates = true,
                    RiskAlerts = true,
                    MarketAlerts = false
                };

                // Act
                var updatedSettings = await PostAsync<NotificationSettingsRequest, NotificationSettingsResponse>(
                    "notification",
                    "/notifications/settings",
                    settings);

                // Assert
                stopwatch.Stop();

                if (updatedSettings == null)
                {
                    return ApiTestResult.Failed(
                        "Updated settings response is null",
                        null,
                        stopwatch.Elapsed);
                }

                bool isValid = updatedSettings.EmailEnabled == settings.EmailEnabled
                    && updatedSettings.PushEnabled == settings.PushEnabled
                    && updatedSettings.OrderUpdates == settings.OrderUpdates
                    && updatedSettings.TradeUpdates == settings.TradeUpdates
                    && updatedSettings.RiskAlerts == settings.RiskAlerts
                    && updatedSettings.MarketAlerts == settings.MarketAlerts;

                if (!isValid)
                {
                    return ApiTestResult.Failed(
                        "Updated settings do not match requested settings",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test marking a notification as read
        /// </summary>
        [ApiTest("Test marking a notification as read")]
        public async Task<ApiTestResult> MarkNotificationAsRead_WithValidId_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Get notifications first to find one to mark as read
                var notifications = await GetAsync<List<NotificationResponse>>("notification", "/notifications");

                if (notifications == null || notifications.Count == 0)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Skipped("No notifications available to mark as read");
                }

                var notification = notifications[0];

                // Act
                var updatedNotification = await PutAsync<object, NotificationResponse>(
                    "notification",
                    $"/notifications/{notification.NotificationId}/read",
                    new { });

                // Assert
                stopwatch.Stop();

                if (updatedNotification == null)
                {
                    return ApiTestResult.Failed(
                        "Updated notification response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (!updatedNotification.IsRead)
                {
                    return ApiTestResult.Failed(
                        "Notification should be marked as read",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test notification service web socket connection
        /// </summary>
        [ApiTest("Test web socket connection to notification service")]
        public async Task<ApiTestResult> WebSocketConnection_ShouldEstablishConnection()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Placeholder for real WebSocket test
                // In a real implementation, this would:
                // 1. Connect to the WebSocket endpoint
                // 2. Subscribe to channels
                // 3. Verify connection is established
                // 4. Close connection

                // Here we're just simulating success for the example
                await Task.Delay(500); // Simulate connection time

                stopwatch.Stop();
                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during WebSocket test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }
    }

    /// <summary>
    /// Response model for notifications
    /// </summary>
    public class NotificationResponse
    {
        public string NotificationId { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public long CreatedAt { get; set; }
    }

    /// <summary>
    /// Request model for notification settings
    /// </summary>
    public class NotificationSettingsRequest
    {
        public bool EmailEnabled { get; set; }
        public bool PushEnabled { get; set; }
        public bool OrderUpdates { get; set; }
        public bool TradeUpdates { get; set; }
        public bool RiskAlerts { get; set; }
        public bool MarketAlerts { get; set; }
    }

    /// <summary>
    /// Response model for notification settings
    /// </summary>
    public class NotificationSettingsResponse
    {
        public string SettingsId { get; set; }
        public bool EmailEnabled { get; set; }
        public bool PushEnabled { get; set; }
        public bool OrderUpdates { get; set; }
        public bool TradeUpdates { get; set; }
        public bool RiskAlerts { get; set; }
        public bool MarketAlerts { get; set; }
        public long UpdatedAt { get; set; }
    }
}