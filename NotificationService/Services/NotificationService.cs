using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using CommonLib.Models.Notification;
using CommonLib.Models;
using CommonLib.Services;

namespace NotificationService.Services
{
    /// <summary>
    /// Implementation of notification service
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;
        private readonly WebSocketService _webSocketService;

        /// <summary>
        /// Initializes a new instance of the NotificationService class
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        /// <param name="webSocketService">WebSocket service for real-time notifications</param>
        public NotificationService(
            MongoDbConnectionFactory dbFactory,
            ILoggerService logger,
            WebSocketService webSocketService)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
        }

        /// <inheritdoc/>
        public async Task<PaginatedResult<Notification>> GetNotificationsAsync(
            ObjectId userId,
            bool includeRead,
            string? type,
            DateTime? startTime,
            DateTime? endTime,
            int page,
            int pageSize)
        {
            try
            {
                var notificationCollection = _dbFactory.GetCollection<Notification>();
                var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);

                if (!includeRead)
                {
                    filter = Builders<Notification>.Filter.And(
                        filter,
                        Builders<Notification>.Filter.Eq(n => n.IsRead, false)
                    );
                }

                // Add type filter if specified
                if (!string.IsNullOrEmpty(type))
                {
                    filter = Builders<Notification>.Filter.And(
                        filter,
                        Builders<Notification>.Filter.Eq(n => n.Type, type)
                    );
                }

                // Add time range filters if specified
                if (startTime.HasValue)
                {
                    filter = Builders<Notification>.Filter.And(
                        filter,
                        Builders<Notification>.Filter.Gte(n => n.CreatedAt, startTime.Value)
                    );
                }

                if (endTime.HasValue)
                {
                    filter = Builders<Notification>.Filter.And(
                        filter,
                        Builders<Notification>.Filter.Lte(n => n.CreatedAt, endTime.Value)
                    );
                }

                // Count total items for pagination
                var totalItems = await notificationCollection.CountDocumentsAsync(filter);

                // Calculate total pages
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                // Get paginated results
                var notifications = await notificationCollection
                    .Find(filter)
                    .Sort(Builders<Notification>.Sort.Descending(n => n.CreatedAt))
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                // Create paginated result
                var result = new PaginatedResult<Notification>
                {
                    Items = notifications,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = (int)totalItems
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving notifications: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Notification?> MarkNotificationAsReadAsync(ObjectId id, ObjectId userId)
        {
            try
            {
                var notificationCollection = _dbFactory.GetCollection<Notification>();
                var filter = Builders<Notification>.Filter.And(
                    Builders<Notification>.Filter.Eq(n => n.Id, id),
                    Builders<Notification>.Filter.Eq(n => n.UserId, userId)
                );

                var update = Builders<Notification>.Update
                    .Set(n => n.IsRead, true)
                    .Set(n => n.ReadAt, DateTime.UtcNow);

                return await notificationCollection.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<Notification> { ReturnDocument = ReturnDocument.After }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking notification as read: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<NotificationSettings> GetNotificationSettingsAsync(ObjectId userId)
        {
            try
            {
                var settingsCollection = _dbFactory.GetCollection<NotificationSettings>();

                var settings = await settingsCollection
                    .Find(s => s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create default settings
                    settings = new NotificationSettings
                    {
                        UserId = userId,
                        EmailEnabled = true,
                        PushEnabled = true,
                        InAppEnabled = true,
                        SmsEnabled = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
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

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving notification settings: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<NotificationSettings> UpdateNotificationSettingsAsync(
            ObjectId userId,
            bool emailEnabled,
            bool pushEnabled,
            Dictionary<string, bool> typeSettings)
        {
            try
            {
                var settingsCollection = _dbFactory.GetCollection<NotificationSettings>();

                var existingSettings = await settingsCollection
                    .Find(s => s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (existingSettings == null)
                {
                    // Create new settings with type conversion from bool to NotificationTypeSettings
                    var notificationTypeSettings = new Dictionary<string, NotificationTypeSettings>();
                    foreach (var kvp in typeSettings)
                    {
                        notificationTypeSettings[kvp.Key] = kvp.Value; // Implicit conversion
                    }

                    // Create new settings
                    existingSettings = new NotificationSettings
                    {
                        UserId = userId,
                        EmailEnabled = emailEnabled,
                        PushEnabled = pushEnabled,
                        TypeSettings = notificationTypeSettings,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await settingsCollection.InsertOneAsync(existingSettings);
                }
                else
                {
                    // Update existing settings
                    var update = Builders<NotificationSettings>.Update
                        .Set(s => s.EmailEnabled, emailEnabled)
                        .Set(s => s.PushEnabled, pushEnabled)
                        .Set(s => s.UpdatedAt, DateTime.UtcNow);

                    // Update type settings if provided
                    foreach (var kvp in typeSettings)
                    {
                        var key = kvp.Key;
                        var enabled = kvp.Value;

                        // Check if type setting already exists
                        if (existingSettings.TypeSettings.ContainsKey(key))
                        {
                            update = update.Set(s => s.TypeSettings[key], enabled);
                        }
                        else
                        {
                            // Add new type setting
                            update = update.Set(s => s.TypeSettings[key], new NotificationTypeSettings
                            {
                                EmailEnabled = enabled,
                                PushEnabled = enabled,
                                InAppEnabled = enabled,
                                SmsEnabled = false
                            });
                        }
                    }

                    // Apply update
                    existingSettings = await settingsCollection.FindOneAndUpdateAsync(
                        s => s.UserId == userId,
                        update,
                        new FindOneAndUpdateOptions<NotificationSettings> { ReturnDocument = ReturnDocument.After }
                    );
                }

                return existingSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating notification settings: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Notification> CreateNotificationAsync(
            ObjectId userId,
            string type,
            string title,
            string message,
            string? relatedId = null,
            string? data = null)
        {
            try
            {
                var notificationCollection = _dbFactory.GetCollection<Notification>();

                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedId = relatedId ?? string.Empty,
                    Data = data ?? "{}"
                };

                await notificationCollection.InsertOneAsync(notification);

                // Send real-time notification
                await SendRealTimeNotificationAsync(userId.ToString(), notification);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating notification: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeleteNotificationsAsync(ObjectId userId, IEnumerable<ObjectId>? notificationIds = null)
        {
            try
            {
                var notificationCollection = _dbFactory.GetCollection<Notification>();
                DeleteResult result;

                if (notificationIds != null && notificationIds.Any())
                {
                    // Delete specific notifications
                    var filter = Builders<Notification>.Filter.And(
                        Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                        Builders<Notification>.Filter.In(n => n.Id, notificationIds)
                    );
                    result = await notificationCollection.DeleteManyAsync(filter);
                }
                else
                {
                    // Delete all read notifications
                    var filter = Builders<Notification>.Filter.And(
                        Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                        Builders<Notification>.Filter.Eq(n => n.IsRead, true)
                    );
                    result = await notificationCollection.DeleteManyAsync(filter);
                }

                return (int)result.DeletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting notifications: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a real-time notification via WebSocket
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="notification">Notification to send</param>
        private async Task SendRealTimeNotificationAsync(string userId, Notification notification)
        {
            try
            {
                // Convert notification to appropriate response format
                var notificationResponse = new
                {
                    Id = notification.Id.ToString(),
                    UserId = notification.UserId.ToString(),
                    Type = notification.Type,
                    Title = notification.Title,
                    Content = notification.Message,
                    IsRead = notification.IsRead,
                    Timestamp = ((DateTimeOffset)notification.CreatedAt).ToUnixTimeMilliseconds(),
                    RelatedId = notification.RelatedId,
                    Data = notification.Data
                };

                await _webSocketService.PublishUserDataUpdate(userId, "notification", notificationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending real-time notification: {ex.Message}");
                // Don't throw - this is a non-critical operation
            }
        }
    }
}