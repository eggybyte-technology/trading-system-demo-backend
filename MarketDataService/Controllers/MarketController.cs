using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Services;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;

namespace MarketDataService.Controllers
{
    /// <summary>
    /// Controller for market data operations
    /// </summary>
    [ApiController]
    [Route("market")]
    public class MarketController : ControllerBase
    {
        private readonly IMarketService _marketService;
        private readonly IKlineService _klineService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly IWebSocketService _webSocketService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Constructor for MarketController
        /// </summary>
        /// <param name="marketService">Market service</param>
        /// <param name="klineService">Kline service</param>
        /// <param name="logger">Logger service</param>
        /// <param name="apiLogger">API logger service</param>
        /// <param name="webSocketService">WebSocket service</param>
        public MarketController(
            IMarketService marketService,
            IKlineService klineService,
            ILoggerService logger,
            IApiLoggingService apiLogger,
            IWebSocketService webSocketService)
        {
            _marketService = marketService ?? throw new ArgumentNullException(nameof(marketService));
            _klineService = klineService ?? throw new ArgumentNullException(nameof(klineService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiLogger = apiLogger ?? throw new ArgumentNullException(nameof(apiLogger));
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
        }

        /// <summary>
        /// Get all available trading symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        [HttpGet("symbols")]
        [ProducesResponseType(typeof(SymbolsResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSymbols()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var symbols = await _marketService.GetSymbolsAsync();

                // Convert business models to response models
                var symbolsResponse = new SymbolsResponse
                {
                    Symbols = symbols.Select(s => new SymbolInfo
                    {
                        Symbol = s.Name,
                        BaseAsset = s.BaseAsset,
                        QuoteAsset = s.QuoteAsset,
                        MinOrderSize = s.MinOrderSize,
                        MaxOrderSize = s.MaxOrderSize,
                        PricePrecision = s.BaseAssetPrecision,
                        QuantityPrecision = s.QuotePrecision,
                        IsActive = s.IsActive
                    }).ToList()
                };

                var response = new { data = symbolsResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting symbols: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while retrieving symbols", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Create a new trading symbol (admin only)
        /// </summary>
        /// <param name="request">Symbol creation details</param>
        /// <returns>Created symbol</returns>
        [HttpPost("symbols")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSymbol([FromBody] SymbolCreateRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var symbol = await _marketService.CreateSymbolAsync(request);
                var response = new SymbolResponse
                {
                    Success = true,
                    Data = new SymbolInfo
                    {
                        Symbol = symbol.Name,
                        BaseAsset = symbol.BaseAsset,
                        QuoteAsset = symbol.QuoteAsset,
                        MinOrderSize = symbol.MinOrderSize,
                        MaxOrderSize = symbol.MaxOrderSize,
                        PricePrecision = (int)symbol.TickSize,
                        QuantityPrecision = (int)symbol.StepSize,
                        IsActive = symbol.IsActive
                    }
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating symbol: {ex.Message}");
                var errorResponse = new SymbolResponse
                {
                    Success = false,
                    Message = "An error occurred creating the symbol"
                };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Update an existing symbol (admin only)
        /// </summary>
        /// <param name="symbolName">Symbol name to update</param>
        /// <param name="request">Symbol update details</param>
        /// <returns>Updated symbol</returns>
        [HttpPut("symbols/{symbolName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSymbol(string symbolName, [FromBody] SymbolUpdateRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var symbol = await _marketService.UpdateSymbolAsync(symbolName, request);
                var response = new SymbolResponse
                {
                    Success = true,
                    Data = new SymbolInfo
                    {
                        Symbol = symbol.Name,
                        BaseAsset = symbol.BaseAsset,
                        QuoteAsset = symbol.QuoteAsset,
                        MinOrderSize = symbol.MinOrderSize,
                        MaxOrderSize = symbol.MaxOrderSize,
                        PricePrecision = (int)symbol.TickSize,
                        QuantityPrecision = (int)symbol.StepSize,
                        IsActive = symbol.IsActive
                    }
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid symbol update: {ex.Message}");
                var errorResponse = new SymbolResponse
                {
                    Success = false,
                    Message = ex.Message
                };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating symbol: {ex.Message}");
                var errorResponse = new SymbolResponse
                {
                    Success = false,
                    Message = "An error occurred updating the symbol"
                };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Process a trade for kline generation
        /// </summary>
        [HttpPost("process-trade")]
        [Authorize(Roles = "Service")]
        public async Task<IActionResult> ProcessTradeForKline([FromBody] TradeForKlineRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var actionStartTime = DateTime.UtcNow;

            try
            {
                // Convert the request to a Trade model
                var trade = new CommonLib.Models.Trading.Trade
                {
                    Id = ObjectId.Empty, // This will be replaced when saved
                    Symbol = request.Symbol,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    IsBuyerMaker = request.IsBuyerMaker,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp).UtcDateTime
                };

                // Process the trade to update klines
                await _klineService.UpdateKlineWithTradeAsync(trade);

                var response = new
                {
                    Success = true,
                    Data = true
                };

                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - actionStartTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing trade for kline: {ex.Message}");
                var errorResponse = new
                {
                    Success = false,
                    Message = "Error processing trade for kline",
                    Data = false
                };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - actionStartTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get market ticker for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <returns>Ticker data</returns>
        [HttpGet("ticker")]
        [ProducesResponseType(typeof(TickerResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTicker([FromQuery] string symbol)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            try
            {
                var marketData = await _marketService.GetTickerAsync(symbol);

                // Convert business model to response model
                var tickerResponse = new TickerResponse
                {
                    Symbol = marketData.Symbol,
                    LastPrice = marketData.LastPrice,
                    PriceChange = marketData.PriceChange,
                    PriceChangePercent = marketData.PriceChangePercent,
                    HighPrice = marketData.High24h,
                    LowPrice = marketData.Low24h,
                    Volume = marketData.Volume24h,
                    QuoteVolume = marketData.QuoteVolume24h,
                    Timestamp = ((DateTimeOffset)marketData.UpdatedAt).ToUnixTimeMilliseconds()
                };

                var response = new { data = tickerResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"Symbol not found: {symbol}");
                var errorResponse = new { message = ex.Message, success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return NotFound(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting ticker for symbol: {symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving ticker data", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get market summary (all tickers)
        /// </summary>
        /// <returns>Market summary data</returns>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(MarketSummaryResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMarketSummary()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var marketDataList = await _marketService.GetMarketSummaryAsync();

                // Convert business models to response model
                var marketSummaryResponse = new MarketSummaryResponse
                {
                    Tickers = marketDataList.Select(md => new TickerResponse
                    {
                        Symbol = md.Symbol,
                        LastPrice = md.LastPrice,
                        PriceChange = md.PriceChange,
                        PriceChangePercent = md.PriceChangePercent,
                        HighPrice = md.High24h,
                        LowPrice = md.Low24h,
                        Volume = md.Volume24h,
                        QuoteVolume = md.QuoteVolume24h,
                        Timestamp = ((DateTimeOffset)md.UpdatedAt).ToUnixTimeMilliseconds()
                    }).ToList(),
                    TotalVolume = marketDataList.Sum(md => md.Volume24h),
                    TotalQuoteVolume = marketDataList.Sum(md => md.QuoteVolume24h),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var response = new { data = marketSummaryResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting market summary: {ex.Message}", ex);
                var errorResponse = new { message = "An error occurred while retrieving market summary", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get market depth (order book)
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Depth limit</param>
        /// <returns>Order book data</returns>
        [HttpGet("depth")]
        [ProducesResponseType(typeof(MarketDepthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDepth([FromQuery] string symbol, [FromQuery] int limit = 100)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (limit <= 0 || limit > 500)
            {
                limit = Math.Min(Math.Max(1, limit), 500);
            }

            try
            {
                var request = new MarketDepthRequest
                {
                    Symbol = symbol,
                    Limit = limit
                };

                var orderBook = await _marketService.GetOrderBookDepthAsync(request);

                // Convert business model to response model
                int actualLimit = Math.Min(limit, 500);
                var marketDepthResponse = new MarketDepthResponse
                {
                    Symbol = orderBook.Symbol,
                    Timestamp = ((DateTimeOffset)orderBook.UpdatedAt).ToUnixTimeMilliseconds(),
                    Bids = orderBook.Bids.Take(actualLimit).Select(p => new decimal[] { p.Price, p.Quantity }).ToList(),
                    Asks = orderBook.Asks.Take(actualLimit).Select(p => new decimal[] { p.Price, p.Quantity }).ToList()
                };

                var response = new { data = marketDepthResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"Order book not found: {symbol}");
                var errorResponse = new { message = ex.Message, success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return NotFound(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order book for symbol: {symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving order book data", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get klines (candlestick) data
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="interval">Interval (1m, 5m, 15m, 30m, 1h, 4h, 1d, 1w)</param>
        /// <param name="limit">Number of klines</param>
        /// <param name="startTime">Start time in milliseconds</param>
        /// <param name="endTime">End time in milliseconds</param>
        /// <returns>Kline data</returns>
        [HttpGet("klines")]
        [ProducesResponseType(typeof(KlineResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKlines(
            [FromQuery] string symbol,
            [FromQuery] string interval,
            [FromQuery] int limit = 500,
            [FromQuery] long? startTime = null,
            [FromQuery] long? endTime = null)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var requestStartTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - requestStartTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(interval))
            {
                var errorResponse = new { message = "Interval is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - requestStartTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (limit <= 0 || limit > 1000)
            {
                limit = Math.Min(Math.Max(1, limit), 1000);
            }

            try
            {
                var request = new KlineRequest
                {
                    Symbol = symbol,
                    Interval = interval,
                    Limit = limit,
                    StartTime = startTime,
                    EndTime = endTime
                };

                var klines = await _marketService.GetKlinesAsync(request);

                // Convert business models to response model
                var formattedKlines = klines.Select(k => new decimal[]
                {
                    ((DateTimeOffset)k.OpenTime).ToUnixTimeMilliseconds(),
                    k.Open,
                    k.High,
                    k.Low,
                    k.Close,
                    k.Volume,
                    ((DateTimeOffset)k.CloseTime).ToUnixTimeMilliseconds(),
                    k.QuoteVolume,
                    k.TradeCount
                }).ToList();

                var klineResponse = new KlineResponse
                {
                    Symbol = request.Symbol,
                    Interval = request.Interval,
                    Klines = formattedKlines
                };

                var response = new { data = klineResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - requestStartTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting klines for symbol: {symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving kline data", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - requestStartTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get recent trades
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Number of trades</param>
        /// <returns>Recent trades</returns>
        [HttpGet("trades")]
        [ProducesResponseType(typeof(TradesResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTrades([FromQuery] string symbol, [FromQuery] int limit = 100)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (limit <= 0 || limit > 1000)
            {
                limit = Math.Min(Math.Max(1, limit), 1000);
            }

            try
            {
                var request = new RecentTradesRequest
                {
                    Symbol = symbol,
                    Limit = limit
                };

                var trades = await _marketService.GetRecentTradesAsync(request);

                // Convert business models to response model
                var tradeResponses = trades.Select(t => new TradeResponse
                {
                    Id = t.Id.ToString(),
                    Price = t.Price,
                    Quantity = t.Quantity,
                    QuoteQuantity = t.Price * t.Quantity,
                    Time = ((DateTimeOffset)t.CreatedAt).ToUnixTimeMilliseconds(),
                    IsBuyerMaker = t.IsBuyerMaker,
                    IsBestMatch = true
                }).ToList();

                var tradesResponse = new TradesResponse
                {
                    Symbol = request.Symbol,
                    Trades = tradeResponses
                };

                var response = new { data = tradesResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent trades for symbol: {symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving recent trades", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }
    }
}