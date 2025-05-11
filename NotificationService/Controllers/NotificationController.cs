using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommonLib.Models.Notification;
using CommonLib.Services;
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
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the notification controller
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public NotificationController(
            MongoDbConnectionFactory dbFactory,
            ILoggerService logger)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all notifications for the current user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <param name="includeRead">Whether to include read notifications (default: false)</param>
        /// <returns>List of notifications</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<Notification>), 200)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeRead = false)
        {
            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notificationCollection = _dbFactory.GetCollection<Notification>();
                var filter = Builders<Notification>.Filter.Eq(n => n.UserId, ObjectId.Parse(userId));

                if (!includeRead)
                {
                    filter = Builders<Notification>.Filter.And(
                        filter,
                        Builders<Notification>.Filter.Eq(n => n.IsRead, false)
                    );
                }

                var notifications = await notificationCollection
                    .Find(filter)
                    .Sort(Builders<Notification>.Sort.Descending(n => n.CreatedAt))
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving notifications: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving notifications");
            }
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>The updated notification</returns>
        [HttpPut("{id}/read")]
        [ProducesResponseType(typeof(Notification), 200)]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notificationCollection = _dbFactory.GetCollection<Notification>();
                var filter = Builders<Notification>.Filter.And(
                    Builders<Notification>.Filter.Eq(n => n.Id, ObjectId.Parse(id)),
                    Builders<Notification>.Filter.Eq(n => n.UserId, ObjectId.Parse(userId))
                );

                var update = Builders<Notification>.Update
                    .Set(n => n.IsRead, true)
                    .Set(n => n.ReadAt, DateTime.UtcNow);

                var result = await notificationCollection.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<Notification> { ReturnDocument = ReturnDocument.After }
                );

                if (result == null)
                {
                    return NotFound("Notification not found or does not belong to the current user");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking notification as read: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the notification");
            }
        }

        /// <summary>
        /// Gets notification settings for the current user
        /// </summary>
        /// <returns>User's notification settings</returns>
        [HttpGet("settings")]
        [ProducesResponseType(typeof(NotificationSettings), 200)]
        public async Task<IActionResult> GetNotificationSettings()
        {
            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var settingsCollection = _dbFactory.GetCollection<NotificationSettings>();
                var userObjectId = ObjectId.Parse(userId);

                var settings = await settingsCollection
                    .Find(s => s.UserId == userObjectId)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create default settings
                    settings = new NotificationSettings
                    {
                        UserId = userObjectId,
                        EmailEnabled = true,
                        PushEnabled = true,
                        TypeSettings = new Dictionary<string, NotificationTypeSettings>
                        {
                            ["ORDER"] = true,
                            ["TRADE"] = true,
                            ["SECURITY"] = true,
                            ["MARKETING"] = false
                        }
                    };

                    await settingsCollection.InsertOneAsync(settings);
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving notification settings: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving notification settings");
            }
        }

        /// <summary>
        /// Updates notification settings for the current user
        /// </summary>
        /// <param name="settings">Updated settings</param>
        /// <returns>Updated notification settings</returns>
        [HttpPost("settings")]
        [ProducesResponseType(typeof(NotificationSettings), 200)]
        public async Task<IActionResult> UpdateNotificationSettings(NotificationSettingsUpdateRequest settings)
        {
            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var settingsCollection = _dbFactory.GetCollection<NotificationSettings>();
                var userObjectId = ObjectId.Parse(userId);

                var existingSettings = await settingsCollection
                    .Find(s => s.UserId == userObjectId)
                    .FirstOrDefaultAsync();

                if (existingSettings == null)
                {
                    // Create new settings with type conversion from bool to NotificationTypeSettings
                    var typeSettings = new Dictionary<string, NotificationTypeSettings>();
                    foreach (var kvp in settings.TypeSettings)
                    {
                        typeSettings[kvp.Key] = kvp.Value; // Implicit conversion
                    }

                    // Create new settings
                    existingSettings = new NotificationSettings
                    {
                        UserId = userObjectId,
                        EmailEnabled = settings.EmailEnabled,
                        PushEnabled = settings.PushEnabled,
                        TypeSettings = typeSettings
                    };

                    await settingsCollection.InsertOneAsync(existingSettings);
                }
                else
                {
                    // Update existing settings with type conversion from bool to NotificationTypeSettings
                    var typeSettings = new Dictionary<string, NotificationTypeSettings>();
                    foreach (var kvp in settings.TypeSettings)
                    {
                        typeSettings[kvp.Key] = kvp.Value; // Implicit conversion
                    }

                    // Update existing settings
                    existingSettings.EmailEnabled = settings.EmailEnabled;
                    existingSettings.PushEnabled = settings.PushEnabled;
                    existingSettings.TypeSettings = typeSettings;

                    await settingsCollection.ReplaceOneAsync(
                        s => s.Id == existingSettings.Id,
                        existingSettings
                    );
                }

                return Ok(existingSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating notification settings: {ex.Message}");
                return StatusCode(500, "An error occurred while updating notification settings");
            }
        }
    }

    /// <summary>
    /// Request model for updating notification settings
    /// </summary>
    public class NotificationSettingsUpdateRequest
    {
        /// <summary>
        /// Whether email notifications are enabled
        /// </summary>
        public bool EmailEnabled { get; set; }

        /// <summary>
        /// Whether push notifications are enabled
        /// </summary>
        public bool PushEnabled { get; set; }

        /// <summary>
        /// Type-specific notification settings
        /// </summary>
        public Dictionary<string, bool> TypeSettings { get; set; } = new Dictionary<string, bool>();
    }
}