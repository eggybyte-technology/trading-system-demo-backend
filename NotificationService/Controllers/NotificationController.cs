using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommonLib.Models;
using CommonLib.Models.Notification;
using CommonLib.Services;
using NotificationService.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NotificationService.Controllers
{
    /// <summary>
    /// Controller for notification operations
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Initializes a new instance of the NotificationController
        /// </summary>
        /// <param name="notificationService">Notification service</param>
        /// <param name="logger">Logger service</param>
        /// <param name="apiLogger">API logger service</param>
        public NotificationController(
            INotificationService notificationService,
            ILoggerService logger,
            IApiLoggingService apiLogger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiLogger = apiLogger ?? throw new ArgumentNullException(nameof(apiLogger));
        }

        /// <summary>
        /// Gets all notifications for the current user
        /// </summary>
        /// <param name="request">Notification query parameters</param>
        /// <returns>List of notifications</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<NotificationResponse>), 200)]
        public async Task<IActionResult> GetNotifications([FromQuery] NotificationQueryRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = new { message = "Unauthorized", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Convert startTime and endTime to DateTime
                DateTime? startTimeDate = request.StartTime.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(request.StartTime.Value).UtcDateTime
                    : null;

                DateTime? endTimeDate = request.EndTime.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(request.EndTime.Value).UtcDateTime
                    : null;

                // Get notifications using service
                var paginatedNotifications = await _notificationService.GetNotificationsAsync(
                    ObjectId.Parse(userId),
                    request.IncludeRead,
                    request.Type,
                    startTimeDate,
                    endTimeDate,
                    request.Page,
                    request.PageSize);

                // Convert to response models
                var responseList = paginatedNotifications.Items.Select(n => new NotificationResponse
                {
                    Id = n.Id.ToString(),
                    UserId = n.UserId.ToString(),
                    Type = n.Type,
                    Title = n.Title,
                    Content = n.Message,
                    IsRead = n.IsRead,
                    Timestamp = new DateTimeOffset(n.CreatedAt).ToUnixTimeMilliseconds(),
                    Data = new Dictionary<string, string> { { "relatedId", n.RelatedId }, { "data", n.Data } }
                }).ToList();

                // Create paginated response
                // Note: TotalPages, HasNextPage, and HasPreviousPage are computed properties
                var paginatedResponse = new PaginatedResult<NotificationResponse>
                {
                    Items = responseList,
                    Page = paginatedNotifications.Page,
                    PageSize = paginatedNotifications.PageSize,
                    TotalItems = paginatedNotifications.TotalItems
                };

                var response = new { data = paginatedResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving notifications: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving notifications", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>The updated notification</returns>
        [HttpPut("{id}/read")]
        [ProducesResponseType(typeof(NotificationResponse), 200)]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = new { message = "Unauthorized", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Mark notification as read using service
                var notification = await _notificationService.MarkNotificationAsReadAsync(
                    ObjectId.Parse(id),
                    ObjectId.Parse(userId));

                if (notification == null)
                {
                    var errorResponse = new { message = "Notification not found or does not belong to the current user", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Convert to response model
                var response = new
                {
                    data = new NotificationResponse
                    {
                        Id = notification.Id.ToString(),
                        UserId = notification.UserId.ToString(),
                        Type = notification.Type,
                        Title = notification.Title,
                        Content = notification.Message,
                        IsRead = notification.IsRead,
                        Timestamp = new DateTimeOffset(notification.CreatedAt).ToUnixTimeMilliseconds(),
                        Data = new Dictionary<string, string> { { "relatedId", notification.RelatedId }, { "data", notification.Data } }
                    },
                    success = true
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking notification as read: {ex.Message}");
                var errorResponse = new { message = "An error occurred while updating the notification", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets notification settings for the current user
        /// </summary>
        /// <returns>User's notification settings</returns>
        [HttpGet("settings")]
        [ProducesResponseType(typeof(NotificationSettingsResponse), 200)]
        public async Task<IActionResult> GetNotificationSettings()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = new { message = "Unauthorized", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Get notification settings using service
                var settings = await _notificationService.GetNotificationSettingsAsync(ObjectId.Parse(userId));

                // Convert to response model
                var response = new
                {
                    data = new NotificationSettingsResponse
                    {
                        UserId = settings.UserId.ToString(),
                        EmailNotifications = settings.EmailEnabled,
                        PushNotifications = settings.PushEnabled,
                        OrderNotifications = settings.TypeSettings.TryGetValue("ORDER", out var orderSettings) && orderSettings.EmailEnabled,
                        TradeNotifications = settings.TypeSettings.TryGetValue("TRADE", out var tradeSettings) && tradeSettings.EmailEnabled,
                        AccountNotifications = settings.TypeSettings.TryGetValue("ACCOUNT", out var accountSettings) && accountSettings.EmailEnabled,
                        SystemNotifications = settings.TypeSettings.TryGetValue("SECURITY", out var securitySettings) && securitySettings.EmailEnabled
                    },
                    success = true
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving notification settings: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving notification settings", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Updates notification settings for the current user
        /// </summary>
        /// <param name="request">Updated settings</param>
        /// <returns>The updated settings</returns>
        [HttpPost("settings")]
        [ProducesResponseType(typeof(NotificationSettingsResponse), 200)]
        public async Task<IActionResult> UpdateNotificationSettings(NotificationSettingsUpdateRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = new { message = "Unauthorized", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Create type settings dictionary
                var typeSettings = request.TypeSettings ?? new Dictionary<string, bool>();

                // Update notification settings using service
                var settings = await _notificationService.UpdateNotificationSettingsAsync(
                    ObjectId.Parse(userId),
                    request.EmailEnabled,
                    request.PushEnabled,
                    typeSettings);

                // Convert to response model
                var response = new
                {
                    data = new NotificationSettingsResponse
                    {
                        UserId = settings.UserId.ToString(),
                        EmailNotifications = settings.EmailEnabled,
                        PushNotifications = settings.PushEnabled,
                        OrderNotifications = settings.TypeSettings.TryGetValue("ORDER", out var orderSettings) && orderSettings.EmailEnabled,
                        TradeNotifications = settings.TypeSettings.TryGetValue("TRADE", out var tradeSettings) && tradeSettings.EmailEnabled,
                        AccountNotifications = settings.TypeSettings.TryGetValue("ACCOUNT", out var accountSettings) && accountSettings.EmailEnabled,
                        SystemNotifications = settings.TypeSettings.TryGetValue("SECURITY", out var securitySettings) && securitySettings.EmailEnabled
                    },
                    success = true
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating notification settings: {ex.Message}");
                var errorResponse = new { message = "An error occurred while updating notification settings", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Deletes notifications
        /// </summary>
        /// <param name="ids">Optional notification IDs to delete</param>
        /// <returns>Number of notifications deleted</returns>
        [HttpDelete]
        [ProducesResponseType(typeof(DeleteNotificationsResponse), 200)]
        public async Task<IActionResult> DeleteNotifications([FromQuery] string? ids)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    var errorResponse = new { message = "Unauthorized", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Parse notification IDs if provided
                IEnumerable<ObjectId>? notificationIds = null;
                if (!string.IsNullOrEmpty(ids))
                {
                    notificationIds = ids.Split(',')
                        .Where(id => !string.IsNullOrEmpty(id) && ObjectId.TryParse(id.Trim(), out _))
                        .Select(id => ObjectId.Parse(id.Trim()));
                }

                // Delete notifications using service
                var count = await _notificationService.DeleteNotificationsAsync(ObjectId.Parse(userId), notificationIds);

                // Create response
                var response = new
                {
                    data = new DeleteNotificationsResponse
                    {
                        DeletedCount = count
                    },
                    success = true
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting notifications: {ex.Message}");
                var errorResponse = new { message = "An error occurred while deleting notifications", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }
    }
}