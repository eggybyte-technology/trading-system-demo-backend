using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommonLib.Models.Notification;
using CommonLib.Services;
using System.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Text.Json;

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
        private readonly IApiLoggingService _apiLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Constructor for WebSocketController
        /// </summary>
        /// <param name="webSocketService">WebSocket service</param>
        /// <param name="logger">Logger service</param>
        /// <param name="jwtService">JWT service</param>
        /// <param name="apiLogger">API logger service</param>
        public WebSocketController(
            WebSocketService webSocketService,
            ILoggerService logger,
            JwtService jwtService,
            IApiLoggingService apiLogger)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _apiLogger = apiLogger ?? throw new ArgumentNullException(nameof(apiLogger));
        }

        /// <summary>
        /// WebSocket endpoint for real-time communication
        /// </summary>
        /// <param name="request">WebSocket connection request (from query parameters)</param>
        [HttpGet]
        [Route("ws")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task Get([FromQuery] WebSocketConnectionRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                if (HttpContext.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _logger.LogInformation("WebSocket connection accepted");

                    // Get token from query string if available
                    string? userId = null;
                    if (!string.IsNullOrEmpty(request.Token))
                    {
                        try
                        {
                            var principal = _jwtService.ValidateToken(request.Token);
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

                    // Log response before handling WebSocket (since we can't log after the connection is established)
                    var response = new { message = "WebSocket connection established", success = true };
                    var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                    // Handle WebSocket connection (this will only return when the connection is closed)
                    await _webSocketService.HandleWebSocketConnection(webSocket, userId ?? string.Empty);
                }
                else
                {
                    HttpContext.Response.StatusCode = 400;
                    _logger.LogWarning("Non-WebSocket request received on WebSocket endpoint");

                    var errorResponse = new { message = "WebSocket request expected", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling WebSocket request: {ex.Message}");
                var errorResponse = new { message = "An error occurred while processing WebSocket request", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                HttpContext.Response.StatusCode = 500;
            }
        }
    }
}