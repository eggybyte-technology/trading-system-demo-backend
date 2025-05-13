using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CommonLib.Models;
using CommonLib.Models.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    public class NotificationService : BaseService
    {
        public NotificationService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "NotificationService", "http://notification.trading-system.local", logger)
        {
        }

        public async Task<PaginatedResult<NotificationResponse>> GetNotificationsAsync(string token, NotificationQueryRequest request)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = request.Page.ToString(),
                ["pageSize"] = request.PageSize.ToString(),
                ["includeRead"] = request.IncludeRead.ToString().ToLower(),
                ["type"] = request.Type,
                ["startTime"] = request.StartTime?.ToString(),
                ["endTime"] = request.EndTime?.ToString()
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<PaginatedResult<NotificationResponse>>($"/notification?{queryString}", token);
        }

        public async Task<NotificationResponse> MarkNotificationAsReadAsync(string token, string notificationId)
        {
            return await PutAsync<NotificationResponse, object>($"/notification/{notificationId}/read", null, token);
        }

        public async Task<NotificationSettingsResponse> GetNotificationSettingsAsync(string token)
        {
            return await GetAsync<NotificationSettingsResponse>("/notification/settings", token);
        }

        public async Task<NotificationSettingsResponse> UpdateNotificationSettingsAsync(string token, NotificationSettingsUpdateRequest request)
        {
            return await PostAsync<NotificationSettingsResponse, NotificationSettingsUpdateRequest>("/notification/settings", request, token);
        }

        public async Task<DeleteNotificationsResponse> DeleteNotificationsAsync(string token, List<string> notificationIds)
        {
            var idsParam = string.Join(",", notificationIds);
            return await DeleteAsync<DeleteNotificationsResponse>($"/notification?ids={idsParam}", token);
        }

        /// <summary>
        /// Get the WebSocket connection URL for real-time notifications
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns>WebSocket connection information</returns>
        public string GetWebSocketUrl(string token)
        {
            var baseAddress = _httpClient.BaseAddress?.ToString().TrimEnd('/');
            if (string.IsNullOrEmpty(baseAddress))
            {
                throw new InvalidOperationException("NotificationService base address is not configured");
            }

            // Convert HTTP URL to WebSocket URL
            var wsBaseUrl = baseAddress.Replace("http://", "ws://").Replace("https://", "wss://");
            return $"{wsBaseUrl}/notification/ws?token={Uri.EscapeDataString(token)}";
        }

        /// <summary>
        /// Creates an HTTP request with headers for establishing a WebSocket connection
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns>WebSocket connection request with headers</returns>
        public WebSocketConnectionRequest CreateWebSocketConnectionRequest(string token)
        {
            return new WebSocketConnectionRequest
            {
                Token = token
            };
        }
    }
}