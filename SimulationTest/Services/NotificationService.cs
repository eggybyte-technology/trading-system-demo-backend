using CommonLib.Models;
using CommonLib.Models.Notification;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Implementation of the notification service for notification operations
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IHttpClientService _httpClient;
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the NotificationService
        /// </summary>
        /// <param name="httpClient">HTTP client service</param>
        public NotificationService(IHttpClientService httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Gets notifications for the current user
        /// </summary>
        /// <param name="queryParams">Query parameters</param>
        /// <returns>Notifications with pagination</returns>
        public async Task<PaginatedResult<Notification>> GetNotificationsAsync(NotificationQueryParams queryParams)
        {
            var query = $"page={queryParams.Page}&pageSize={queryParams.PageSize}";

            if (queryParams.ReadStatus.HasValue)
                query += $"&readStatus={queryParams.ReadStatus.Value}";

            if (!string.IsNullOrEmpty(queryParams.Type))
                query += $"&type={queryParams.Type}";

            if (queryParams.StartTime.HasValue)
                query += $"&startTime={queryParams.StartTime.Value}";

            if (queryParams.EndTime.HasValue)
                query += $"&endTime={queryParams.EndTime.Value}";

            return await _httpClient.GetAsync<PaginatedResult<Notification>>("notification", "notifications", query);
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>Updated notification</returns>
        public async Task<Notification> MarkNotificationAsReadAsync(string notificationId)
        {
            return await _httpClient.PutAsync<object, Notification>("notification", $"notifications/{notificationId}/read", null);
        }

        /// <summary>
        /// Updates notification settings
        /// </summary>
        /// <param name="settings">Notification settings</param>
        /// <returns>Updated notification settings</returns>
        public async Task<NotificationSettings> UpdateNotificationSettingsAsync(NotificationSettings settings)
        {
            return await _httpClient.PostAsync<NotificationSettings, NotificationSettings>("notification", "notifications/settings", settings);
        }

        /// <summary>
        /// Connects to the WebSocket for real-time notifications
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="onMessageReceived">Action to execute when a message is received</param>
        public async Task ConnectToWebSocketAsync(string token, Action<string> onMessageReceived)
        {
            // Disconnect if already connected
            await DisconnectFromWebSocketAsync();

            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            var uri = new Uri($"ws://notification.trading-system.local/ws?token={token}");
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

            // Start listening for messages
            _ = Task.Run(async () =>
            {
                var buffer = new byte[4096];

                try
                {
                    while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            onMessageReceived?.Invoke(message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // Handle WebSocket errors
                    await DisconnectFromWebSocketAsync();
                }
            });
        }

        /// <summary>
        /// Disconnects from the WebSocket
        /// </summary>
        public async Task DisconnectFromWebSocketAsync()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                catch (Exception)
                {
                    // Ignore errors during disconnection
                }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Subscribes to WebSocket channels
        /// </summary>
        /// <param name="channels">List of channels</param>
        /// <param name="symbols">List of symbols</param>
        public async Task SubscribeToChannelsAsync(string[] channels, string[] symbols)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket is not connected");

            var subscriptionMessage = new
            {
                type = "SUBSCRIBE",
                channels,
                symbols
            };

            var json = JsonSerializer.Serialize(subscriptionMessage, _jsonOptions);
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }
    }
}