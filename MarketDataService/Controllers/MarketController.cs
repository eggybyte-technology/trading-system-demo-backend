using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Services;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataService.Controllers
{
    /// <summary>
    /// Controller for market data operations
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MarketController : ControllerBase
    {
        private readonly IMarketService _marketService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Constructor for MarketController
        /// </summary>
        /// <param name="marketService">Market service</param>
        /// <param name="logger">Logger service</param>
        /// <param name="apiLogger">API logger service</param>
        public MarketController(
            IMarketService marketService,
            ILoggerService logger,
            IApiLoggingService apiLogger)
        {
            _marketService = marketService ?? throw new ArgumentNullException(nameof(marketService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiLogger = apiLogger ?? throw new ArgumentNullException(nameof(apiLogger));
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
        /// Get ticker information for a specific symbol
        /// </summary>
        /// <param name="request">Ticker request parameters</param>
        /// <returns>Ticker information</returns>
        [HttpGet("ticker")]
        [ProducesResponseType(typeof(TickerResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTicker([FromQuery] TickerRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.Symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            try
            {
                var marketData = await _marketService.GetTickerAsync(request.Symbol);

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
                _logger.LogWarning($"Symbol not found: {request.Symbol}");
                var errorResponse = new { message = ex.Message, success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return NotFound(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting ticker for symbol: {request.Symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving ticker data", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get market summary for all symbols
        /// </summary>
        /// <returns>Market summary</returns>
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
        /// Get order book depth for a symbol
        /// </summary>
        /// <param name="request">Market depth request parameters</param>
        /// <returns>Order book depth</returns>
        [HttpGet("depth")]
        [ProducesResponseType(typeof(MarketDepthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMarketDepth([FromQuery] MarketDepthRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.Symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (request.Limit <= 0 || request.Limit > 500)
            {
                request.Limit = Math.Min(Math.Max(1, request.Limit), 500);
            }

            try
            {
                var orderBook = await _marketService.GetMarketDepthAsync(request);

                // Convert business model to response model
                int limit = Math.Min(request.Limit, 500);
                var marketDepthResponse = new MarketDepthResponse
                {
                    Symbol = orderBook.Symbol,
                    Timestamp = ((DateTimeOffset)orderBook.UpdatedAt).ToUnixTimeMilliseconds(),
                    Bids = orderBook.Bids.Take(limit).Select(p => new decimal[] { p.Price, p.Quantity }).ToList(),
                    Asks = orderBook.Asks.Take(limit).Select(p => new decimal[] { p.Price, p.Quantity }).ToList()
                };

                var response = new { data = marketDepthResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"Order book not found: {request.Symbol}");
                var errorResponse = new { message = ex.Message, success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return NotFound(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order book for symbol: {request.Symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving order book data", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get kline/candlestick data for a symbol
        /// </summary>
        /// <param name="request">Kline request parameters</param>
        /// <returns>Kline data</returns>
        [HttpGet("klines")]
        [ProducesResponseType(typeof(KlineResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKlines([FromQuery] KlineRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.Symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.Interval))
            {
                var errorResponse = new { message = "Interval is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (request.Limit <= 0 || request.Limit > 1000)
            {
                request.Limit = Math.Min(Math.Max(1, request.Limit), 1000);
            }

            try
            {
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
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting klines for symbol: {request.Symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving kline data", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Get recent trades for a symbol
        /// </summary>
        /// <param name="request">Recent trades request parameters</param>
        /// <returns>Recent trades</returns>
        [HttpGet("trades")]
        [ProducesResponseType(typeof(TradesResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRecentTrades([FromQuery] RecentTradesRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(request.Symbol))
            {
                var errorResponse = new { message = "Symbol is required", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }

            if (request.Limit <= 0 || request.Limit > 1000)
            {
                request.Limit = Math.Min(Math.Max(1, request.Limit), 1000);
            }

            try
            {
                var trades = await _marketService.GetRecentTradesAsync(request.Symbol, request.Limit);

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
                _logger.LogError($"Error getting recent trades for symbol: {request.Symbol}", ex);
                var errorResponse = new { message = "An error occurred while retrieving recent trades", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }
    }
}