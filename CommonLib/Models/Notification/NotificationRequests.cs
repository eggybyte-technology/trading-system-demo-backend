using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace CommonLib.Models.Notification
{
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

    /// <summary>
    /// WebSocket subscription request
    /// </summary>
    public class WebSocketSubscriptionRequest
    {
        /// <summary>
        /// Subscription type (SUBSCRIBE, UNSUBSCRIBE)
        /// </summary>
        public string Type { get; set; } = "SUBSCRIBE";

        /// <summary>
        /// Channels to subscribe to
        /// </summary>
        public string[] Channels { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Symbols to subscribe to
        /// </summary>
        public string[] Symbols { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// WebSocket connection request
    /// </summary>
    public class WebSocketConnectionRequest
    {
        /// <summary>
        /// Authentication token
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Initial subscription channels (optional)
        /// </summary>
        public string[]? Channels { get; set; }

        /// <summary>
        /// Initial subscription symbols (optional)
        /// </summary>
        public string[]? Symbols { get; set; }
    }

    /// <summary>
    /// Request model for querying notifications
    /// </summary>
    public class NotificationQueryRequest
    {
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Whether to include read notifications
        /// </summary>
        public bool IncludeRead { get; set; } = false;

        /// <summary>
        /// Optional notification type filter
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Optional start time filter (Unix timestamp in milliseconds)
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// Optional end time filter (Unix timestamp in milliseconds)
        /// </summary>
        public long? EndTime { get; set; }
    }
}