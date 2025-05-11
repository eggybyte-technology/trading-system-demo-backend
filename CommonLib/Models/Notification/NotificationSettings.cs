using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Notification
{
    /// <summary>
    /// User notification settings
    /// </summary>
    public class NotificationSettings : IndexedModel<NotificationSettings>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "NotificationDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "NotificationSettings";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Email notifications enabled
        /// </summary>
        [BsonElement("emailEnabled")]
        public bool EmailEnabled { get; set; } = true;

        /// <summary>
        /// Email notifications enabled (obsolete, use EmailEnabled instead)
        /// </summary>
        [Obsolete("Use EmailEnabled instead")]
        public bool EmailNotificationsEnabled
        {
            get => EmailEnabled;
            set => EmailEnabled = value;
        }

        /// <summary>
        /// Push notifications enabled
        /// </summary>
        [BsonElement("pushEnabled")]
        public bool PushEnabled { get; set; } = true;

        /// <summary>
        /// Push notifications enabled (obsolete, use PushEnabled instead)
        /// </summary>
        [Obsolete("Use PushEnabled instead")]
        public bool PushNotificationsEnabled
        {
            get => PushEnabled;
            set => PushEnabled = value;
        }

        /// <summary>
        /// In-app notifications enabled
        /// </summary>
        [BsonElement("inAppEnabled")]
        public bool InAppEnabled { get; set; } = true;

        /// <summary>
        /// SMS notifications enabled
        /// </summary>
        [BsonElement("smsEnabled")]
        public bool SmsEnabled { get; set; } = false;

        /// <summary>
        /// Notification settings for different types
        /// </summary>
        [BsonElement("typeSettings")]
        public Dictionary<string, NotificationTypeSettings> TypeSettings { get; set; } = new();

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<NotificationSettings>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<NotificationSettings>, bool>>
            {
                new Tuple<IndexKeysDefinition<NotificationSettings>, bool>(
                    Builders<NotificationSettings>.IndexKeys.Ascending(s => s.UserId),
                    true
                )
            };
        }
    }

    /// <summary>
    /// Settings for a specific notification type
    /// </summary>
    public class NotificationTypeSettings
    {
        /// <summary>
        /// Email notifications enabled for this type
        /// </summary>
        [BsonElement("emailEnabled")]
        public bool EmailEnabled { get; set; } = true;

        /// <summary>
        /// Push notifications enabled for this type
        /// </summary>
        [BsonElement("pushEnabled")]
        public bool PushEnabled { get; set; } = true;

        /// <summary>
        /// In-app notifications enabled for this type
        /// </summary>
        [BsonElement("inAppEnabled")]
        public bool InAppEnabled { get; set; } = true;

        /// <summary>
        /// SMS notifications enabled for this type
        /// </summary>
        [BsonElement("smsEnabled")]
        public bool SmsEnabled { get; set; } = false;

        /// <summary>
        /// Implicit conversion from bool to NotificationTypeSettings
        /// </summary>
        /// <param name="enabled">Enabled state</param>
        public static implicit operator NotificationTypeSettings(bool enabled)
        {
            return new NotificationTypeSettings
            {
                EmailEnabled = enabled,
                PushEnabled = enabled,
                InAppEnabled = enabled
            };
        }
    }
}