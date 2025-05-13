using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace CommonLib.Models.Notification
{
    /// <summary>
    /// Response model for a notification
    /// </summary>
    public class NotificationResponse
    {
        /// <summary>
        /// Notification ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Notification type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Notification title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notification content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether the notification has been read
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Notification creation timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Reference data associated with the notification
        /// </summary>
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Response model for notification settings
    /// </summary>
    public class NotificationSettingsResponse
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Whether email notifications are enabled
        /// </summary>
        public bool EmailNotifications { get; set; }

        /// <summary>
        /// Whether push notifications are enabled
        /// </summary>
        public bool PushNotifications { get; set; }

        /// <summary>
        /// Whether order notifications are enabled
        /// </summary>
        public bool OrderNotifications { get; set; }

        /// <summary>
        /// Whether trade notifications are enabled
        /// </summary>
        public bool TradeNotifications { get; set; }

        /// <summary>
        /// Whether account notifications are enabled
        /// </summary>
        public bool AccountNotifications { get; set; }

        /// <summary>
        /// Whether system notifications are enabled
        /// </summary>
        public bool SystemNotifications { get; set; }
    }

    /// <summary>
    /// WebSocket message response
    /// </summary>
    public class WebSocketMessageResponse
    {
        /// <summary>
        /// Channel name
        /// </summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// Symbol name (if applicable)
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Message data
        /// </summary>
        public object Data { get; set; } = new();

        /// <summary>
        /// Message timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for delete notifications operation
    /// </summary>
    public class DeleteNotificationsResponse
    {
        /// <summary>
        /// Number of notifications deleted
        /// </summary>
        public int DeletedCount { get; set; }
    }
}