using CommonLib.Models;
using CommonLib.Models.Notification;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for notification operations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets notifications for the current user
        /// </summary>
        /// <param name="queryParams">Query parameters</param>
        /// <returns>Notifications with pagination</returns>
        Task<PaginatedResult<Notification>> GetNotificationsAsync(NotificationQueryParams queryParams);

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>Updated notification</returns>
        Task<Notification> MarkNotificationAsReadAsync(string notificationId);

        /// <summary>
        /// Updates notification settings
        /// </summary>
        /// <param name="settings">Notification settings</param>
        /// <returns>Updated notification settings</returns>
        Task<NotificationSettings> UpdateNotificationSettingsAsync(NotificationSettings settings);
    }
}