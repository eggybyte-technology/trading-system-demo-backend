using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommonLib.Services;
using System.Threading.Tasks;
using System;
using System.Net.WebSockets;

namespace NotificationService.Controllers
{
    /// <summary>
    /// Controller for WebSocket connections
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly WebSocketService _webSocketService;
        private readonly ILoggerService _logger;
        private readonly JwtService _jwtService;

        /// <summary>
        /// Constructor for WebSocketController
        /// </summary>
        public WebSocketController(
            WebSocketService webSocketService,
            ILoggerService logger,
            JwtService jwtService)
        {
            _webSocketService = webSocketService;
            _logger = logger;
            _jwtService = jwtService;
        }

        /// <summary>
        /// WebSocket endpoint for real-time communication
        /// </summary>
        [HttpGet]
        [Route("ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _logger.LogInformation("WebSocket connection accepted");

                // Get token from query string if available
                string? userId = null;
                if (HttpContext.Request.Query.TryGetValue("token", out var tokenValue))
                {
                    try
                    {
                        var token = tokenValue.ToString();
                        var principal = _jwtService.ValidateToken(token);
                        userId = principal?.Identity?.Name;

                        if (!string.IsNullOrEmpty(userId))
                        {
                            _logger.LogInformation($"Authenticated WebSocket connection for user {userId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Invalid token in WebSocket connection: {ex.Message}");
                    }
                }

                // Handle WebSocket connection
                await _webSocketService.HandleWebSocketConnection(webSocket, userId ?? string.Empty);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
                _logger.LogWarning("Non-WebSocket request received on WebSocket endpoint");
            }
        }
    }
}