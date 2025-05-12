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
}