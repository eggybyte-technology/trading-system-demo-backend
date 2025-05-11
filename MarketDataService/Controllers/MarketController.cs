using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
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
        private readonly ILogger<MarketController> _logger;

        /// <summary>
        /// Constructor for MarketController
        /// </summary>
        /// <param name="marketService">Market service</param>
        /// <param name="logger">Logger</param>
        public MarketController(IMarketService marketService, ILogger<MarketController> logger)
        {
            _marketService = marketService ?? throw new ArgumentNullException(nameof(marketService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all available trading symbols
        /// </summary>
        /// <returns>List of symbols</returns>
        [HttpGet("symbols")]
        [ProducesResponseType(typeof(SymbolsResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SymbolsResponse>> GetSymbols()
        {
            try
            {
                var result = await _marketService.GetSymbolsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symbols");
                return StatusCode(500, "An error occurred while retrieving symbols");
            }
        }

        /// <summary>
        /// Get ticker information for a specific symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <returns>Ticker information</returns>
        [HttpGet("ticker")]
        [ProducesResponseType(typeof(TickerResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TickerResponse>> GetTicker([FromQuery] string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return BadRequest("Symbol is required");
            }

            try
            {
                var result = await _marketService.GetTickerAsync(symbol);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Symbol not found: {Symbol}", symbol);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticker for symbol: {Symbol}", symbol);
                return StatusCode(500, "An error occurred while retrieving ticker data");
            }
        }

        /// <summary>
        /// Get market summary for all symbols
        /// </summary>
        /// <returns>Market summary</returns>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(MarketSummaryResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<MarketSummaryResponse>> GetMarketSummary()
        {
            try
            {
                var result = await _marketService.GetMarketSummaryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market summary");
                return StatusCode(500, "An error occurred while retrieving market summary");
            }
        }

        /// <summary>
        /// Get order book depth for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Maximum number of price levels to return (default: 100, max: 500)</param>
        /// <returns>Order book depth</returns>
        [HttpGet("depth")]
        [ProducesResponseType(typeof(MarketDepthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<MarketDepthResponse>> GetMarketDepth([FromQuery] string symbol, [FromQuery] int limit = 100)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return BadRequest("Symbol is required");
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

                var result = await _marketService.GetMarketDepthAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order book not found: {Symbol}", symbol);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order book for symbol: {Symbol}", symbol);
                return StatusCode(500, "An error occurred while retrieving order book data");
            }
        }

        /// <summary>
        /// Get kline/candlestick data for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="interval">Kline interval (e.g., 1m, 5m, 1h, 1d)</param>
        /// <param name="startTime">Optional start time in milliseconds</param>
        /// <param name="endTime">Optional end time in milliseconds</param>
        /// <param name="limit">Maximum number of klines to return (default: 500, max: 1000)</param>
        /// <returns>Kline data</returns>
        [HttpGet("klines")]
        [ProducesResponseType(typeof(List<decimal[]>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<decimal[]>>> GetKlines(
            [FromQuery] string symbol,
            [FromQuery] string interval = "1h",
            [FromQuery] long? startTime = null,
            [FromQuery] long? endTime = null,
            [FromQuery] int limit = 500)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return BadRequest("Symbol is required");
            }

            if (string.IsNullOrEmpty(interval))
            {
                return BadRequest("Interval is required");
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
                    StartTime = startTime,
                    EndTime = endTime,
                    Limit = limit
                };

                var result = await _marketService.GetKlinesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting klines for symbol: {Symbol}, interval: {Interval}", symbol, interval);
                return StatusCode(500, "An error occurred while retrieving kline data");
            }
        }

        /// <summary>
        /// Get recent trades for a symbol
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="limit">Maximum number of trades to return (default: 100, max: 1000)</param>
        /// <returns>List of trades</returns>
        [HttpGet("trades")]
        [ProducesResponseType(typeof(List<TradeResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<TradeResponse>>> GetRecentTrades([FromQuery] string symbol, [FromQuery] int limit = 100)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return BadRequest("Symbol is required");
            }

            if (limit <= 0 || limit > 1000)
            {
                limit = Math.Min(Math.Max(1, limit), 1000);
            }

            try
            {
                var result = await _marketService.GetRecentTradesAsync(symbol, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trades for symbol: {Symbol}", symbol);
                return StatusCode(500, "An error occurred while retrieving trade data");
            }
        }
    }
}