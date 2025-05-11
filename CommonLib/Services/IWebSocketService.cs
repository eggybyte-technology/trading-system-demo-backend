using System.Net.WebSockets;
using System.Threading.Tasks;
using CommonLib.Models.Market;

namespace CommonLib.Services
{
    /// <summary>
    /// Service for WebSocket communication
    /// </summary>
    public interface IWebSocketService
    {
        /// <summary>
        /// Sends a message to all connected WebSocket clients or specific client
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="connectionId">Optional specific connection ID</param>
        Task SendMessageAsync(WebSocketMessage message, string? connectionId = null);

        /// <summary>
        /// Publishes a ticker update to subscribers
        /// </summary>
        Task PublishTickerUpdate(string symbol, WebSocketTickerData tickerData);

        /// <summary>
        /// Publishes an order book update to subscribers
        /// </summary>
        Task PublishDepthUpdate(string symbol, WebSocketDepthData depthData);

        /// <summary>
        /// Publishes a trade update to subscribers
        /// </summary>
        Task PublishTradeUpdate(string symbol, WebSocketTradeData tradeData);

        /// <summary>
        /// Publishes a kline/candlestick update to subscribers
        /// </summary>
        Task PublishKlineUpdate(string symbol, string interval, KlineData klineData);

        /// <summary>
        /// Publishes a user-specific data update to a specific user
        /// </summary>
        Task PublishUserDataUpdate(string userId, string subType, object data);
    }
}