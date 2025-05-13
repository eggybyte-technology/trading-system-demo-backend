using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using CommonLib.Models.Notification;
using CommonLib.Models;

namespace NotificationService.Services
{
    /// <summary>
    /// Interface for notification service operations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets notifications for a user with optional filtering
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="includeRead">Whether to include read notifications</param>
        /// <param name="type">Optional notification type filter</param>
        /// <param name="startTime">Optional start time filter</param>
        /// <param name="endTime">Optional end time filter</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated list of notifications</returns>
        Task<PaginatedResult<Notification>> GetNotificationsAsync(
            ObjectId userId,
            bool includeRead,
            string? type,
            DateTime? startTime,
            DateTime? endTime,
            int page,
            int pageSize);

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>The updated notification</returns>
        Task<Notification?> MarkNotificationAsReadAsync(ObjectId id, ObjectId userId);

        /// <summary>
        /// Gets notification settings for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>The user's notification settings</returns>
        Task<NotificationSettings> GetNotificationSettingsAsync(ObjectId userId);

        /// <summary>
        /// Updates notification settings for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="emailEnabled">Whether email notifications are enabled</param>
        /// <param name="pushEnabled">Whether push notifications are enabled</param>
        /// <param name="typeSettings">Type-specific notification settings</param>
        /// <returns>The updated notification settings</returns>
        Task<NotificationSettings> UpdateNotificationSettingsAsync(
            ObjectId userId,
            bool emailEnabled,
            bool pushEnabled,
            Dictionary<string, bool> typeSettings);

        /// <summary>
        /// Creates a new notification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="type">Notification type</param>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="relatedId">Optional related entity ID</param>
        /// <param name="data">Optional additional data</param>
        /// <returns>The created notification</returns>
        Task<Notification> CreateNotificationAsync(
            ObjectId userId,
            string type,
            string title,
            string message,
            string? relatedId = null,
            string? data = null);

        /// <summary>
        /// Deletes notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="notificationIds">Notification IDs to delete, or null to delete all read notifications</param>
        /// <returns>Number of notifications deleted</returns>
        Task<int> DeleteNotificationsAsync(ObjectId userId, IEnumerable<ObjectId>? notificationIds = null);
    }
}