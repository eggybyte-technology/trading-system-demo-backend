using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CommonLib.Models.Market;
using Microsoft.Extensions.Logging;

namespace CommonLib.Services
{
    /// <summary>
    /// Implementation of the WebSocket service for real-time communication
    /// </summary>
    public class WebSocketService : IWebSocketService
    {
        private readonly ILogger<WebSocketService> _logger;
        private readonly ConcurrentDictionary<string, WebSocket> _clients = new ConcurrentDictionary<string, WebSocket>();
        private readonly ConcurrentDictionary<WebSocket, string> _clientIds = new ConcurrentDictionary<WebSocket, string>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _channelSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>>();
        private readonly ConcurrentDictionary<string, HashSet<string>> _userSubscriptions = new ConcurrentDictionary<string, HashSet<string>>();

        public WebSocketService(ILogger<WebSocketService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends a message to all connected WebSocket clients or specific client
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="connectionId">Optional specific connection ID</param>
        public async Task SendMessageAsync(WebSocketMessage message, string? connectionId = null)
        {
            try
            {
                if (connectionId != null)
                {
                    if (_clients.TryGetValue(connectionId, out var socket))
                    {
                        await SendToClientAsync(socket, message);
                    }
                }
                else
                {
                    foreach (var socket in _clients.Values)
                    {
                        await SendToClientAsync(socket, message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
            }
        }

        /// <summary>
        /// Handles a new WebSocket connection
        /// </summary>
        /// <param name="webSocket">Connected WebSocket</param>
        /// <param name="userId">User identifier or null for anonymous</param>
        public async Task HandleWebSocketConnection(WebSocket webSocket, string userId)
        {
            var connectionId = Guid.NewGuid().ToString();
            _clients.TryAdd(connectionId, webSocket);
            _clientIds.TryAdd(webSocket, connectionId);

            if (!string.IsNullOrEmpty(userId))
            {
                _userSubscriptions.TryAdd(userId, new HashSet<string> { connectionId });
            }

            try
            {
                var buffer = new byte[4096];
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleMessageAsync(webSocket, message, userId);
                    }
                }
                while (!result.CloseStatus.HasValue);

                await UnsubscribeClient(webSocket);
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
                await UnsubscribeClient(webSocket);
            }
        }

        /// <summary>
        /// Handles a subscription request from a client
        /// </summary>
        /// <param name="webSocket">Client WebSocket</param>
        /// <param name="request">Subscription request</param>
        /// <param name="userId">User identifier</param>
        public async Task HandleSubscriptionRequest(WebSocket webSocket, SubscriptionRequest request, string userId)
        {
            if (!_clientIds.TryGetValue(webSocket, out var connectionId))
            {
                return;
            }

            foreach (var channel in request.Channels)
            {
                if (!_channelSubscriptions.TryGetValue(channel, out var channelDict))
                {
                    channelDict = new ConcurrentDictionary<string, HashSet<string>>();
                    _channelSubscriptions.TryAdd(channel, channelDict);
                }

                // If symbols are provided, subscribe to specific symbols
                if (request.Symbols != null && request.Symbols.Count > 0)
                {
                    foreach (var symbol in request.Symbols)
                    {
                        if (!channelDict.TryGetValue(symbol, out var subscribers))
                        {
                            subscribers = new HashSet<string>();
                            channelDict.TryAdd(symbol, subscribers);
                        }

                        lock (subscribers)
                        {
                            subscribers.Add(connectionId);
                        }
                    }
                }
                else
                {
                    // Subscribe to all symbols by using an empty string
                    if (!channelDict.TryGetValue("", out var subscribers))
                    {
                        subscribers = new HashSet<string>();
                        channelDict.TryAdd("", subscribers);
                    }

                    lock (subscribers)
                    {
                        subscribers.Add(connectionId);
                    }
                }
            }

            // Acknowledge subscription
            var response = new WebSocketMessage
            {
                Type = "subscribed",
            };

            await SendToClientAsync(webSocket, response);
        }

        /// <summary>
        /// Unsubscribes a client from all channels
        /// </summary>
        /// <param name="webSocket">Client WebSocket</param>
        public async Task UnsubscribeClient(WebSocket webSocket)
        {
            if (!_clientIds.TryRemove(webSocket, out var connectionId))
            {
                return;
            }

            _clients.TryRemove(connectionId, out _);

            // Remove from all channel subscriptions
            foreach (var channelDict in _channelSubscriptions.Values)
            {
                foreach (var subscribers in channelDict.Values)
                {
                    lock (subscribers)
                    {
                        subscribers.Remove(connectionId);
                    }
                }
            }

            // Also remove from user subscriptions
            foreach (var subs in _userSubscriptions.Values)
            {
                lock (subs)
                {
                    subs.Remove(connectionId);
                }
            }
        }

        /// <summary>
        /// Publishes a ticker update to subscribers
        /// </summary>
        /// <param name="symbol">Trading pair symbol</param>
        /// <param name="tickerData">Ticker data to publish</param>
        public async Task PublishTickerUpdate(string symbol, WebSocketTickerData tickerData)
        {
            var message = new WebSocketMessage
            {
                Type = "ticker",
                Symbol = symbol,
                Data = tickerData
            };

            await BroadcastToChannelSubscribers("ticker", symbol, message);
        }

        /// <summary>
        /// Publishes an order book update to subscribers
        /// </summary>
        /// <param name="symbol">Trading pair symbol</param>
        /// <param name="depthData">Depth data to publish</param>
        public async Task PublishDepthUpdate(string symbol, WebSocketDepthData depthData)
        {
            var message = new WebSocketMessage
            {
                Type = "depth",
                Symbol = symbol,
                Data = depthData
            };

            await BroadcastToChannelSubscribers("depth", symbol, message);
        }

        /// <summary>
        /// Publishes a trade update to subscribers
        /// </summary>
        /// <param name="symbol">Trading pair symbol</param>
        /// <param name="tradeData">Trade data to publish</param>
        public async Task PublishTradeUpdate(string symbol, WebSocketTradeData tradeData)
        {
            var message = new WebSocketMessage
            {
                Type = "trade",
                Symbol = symbol,
                Data = tradeData
            };

            await BroadcastToChannelSubscribers("trade", symbol, message);
        }

        /// <summary>
        /// Publishes a kline/candlestick update to subscribers
        /// </summary>
        /// <param name="symbol">Trading pair symbol</param>
        /// <param name="interval">Kline interval</param>
        /// <param name="klineData">Kline data to publish</param>
        public async Task PublishKlineUpdate(string symbol, string interval, KlineData klineData)
        {
            var message = new WebSocketMessage
            {
                Type = "kline",
                Symbol = symbol,
                Data = new
                {
                    Interval = interval,
                    Kline = klineData
                }
            };

            await BroadcastToChannelSubscribers("kline", symbol, message);
        }

        /// <summary>
        /// Publishes a user-specific data update to a specific user
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="subType">Data subtype</param>
        /// <param name="data">Data payload</param>
        public async Task PublishUserDataUpdate(string userId, string subType, object data)
        {
            var message = new WebSocketMessage
            {
                Type = "userData",
                Data = new
                {
                    SubType = subType,
                    Data = data
                }
            };

            if (_userSubscriptions.TryGetValue(userId, out var subscribers))
            {
                await SendToSubscribersAsync(subscribers, message);
            }
        }

        /// <summary>
        /// Handles incoming WebSocket messages
        /// </summary>
        private async Task HandleMessageAsync(WebSocket webSocket, string message, string userId)
        {
            try
            {
                var request = JsonSerializer.Deserialize<WebSocketMessage>(message);
                if (request == null) return;

                if (request.Type == "subscribe")
                {
                    var subscriptionRequest = JsonSerializer.Deserialize<SubscriptionRequest>(message);
                    if (subscriptionRequest != null)
                    {
                        await HandleSubscriptionRequest(webSocket, subscriptionRequest, userId);
                    }
                }
                // Handle other message types as needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket message");
            }
        }

        /// <summary>
        /// Sends a message to a specific WebSocket client
        /// </summary>
        private async Task SendToClientAsync<T>(WebSocket socket, T message)
        {
            if (socket.State != WebSocketState.Open) return;

            try
            {
                var json = JsonSerializer.Serialize(message);
                var buffer = Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending to client");
            }
        }

        /// <summary>
        /// Sends a message to a list of subscribers
        /// </summary>
        private async Task SendToSubscribersAsync<T>(HashSet<string> subscribers, T message)
        {
            var tasks = new List<Task>();

            foreach (var connectionId in subscribers)
            {
                if (_clients.TryGetValue(connectionId, out var socket))
                {
                    tasks.Add(SendToClientAsync(socket, message));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Broadcasts a message to subscribers of a specific channel and symbol
        /// </summary>
        private async Task BroadcastToChannelSubscribers<T>(string channel, string symbol, T message)
        {
            if (!_channelSubscriptions.TryGetValue(channel, out var symbolDict)) return;

            // Send to subscribers of the specific symbol
            if (symbolDict.TryGetValue(symbol, out var subscribers))
            {
                await SendToSubscribersAsync(subscribers, message);
            }

            // Also send to subscribers of all symbols (empty string)
            if (symbolDict.TryGetValue("", out var allSubscribers))
            {
                await SendToSubscribersAsync(allSubscribers, message);
            }
        }
    }
}