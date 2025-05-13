using CommonLib.Models.Market;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MarketDataService.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketDataService.Services
{
    /// <summary>
    /// Implementation of IKlineService for Kline generation and management
    /// </summary>
    public class KlineService : IKlineService
    {
        private readonly IKlineRepository _klineRepository;
        private readonly ITradeRepository _tradeRepository;
        private readonly ISymbolRepository _symbolRepository;
        private readonly IWebSocketService _webSocketService;
        private readonly ILoggerService _logger;
        private readonly ILogger<KlineService> _ilogger;

        private static readonly string[] _supportedIntervals = new[] { "1m", "5m", "15m", "30m", "1h", "4h", "1d", "1w" };

        /// <summary>
        /// Constructor
        /// </summary>
        public KlineService(
            IKlineRepository klineRepository,
            ITradeRepository tradeRepository,
            ISymbolRepository symbolRepository,
            IWebSocketService webSocketService,
            ILoggerService logger,
            ILogger<KlineService> ilogger)
        {
            _klineRepository = klineRepository ?? throw new ArgumentNullException(nameof(klineRepository));
            _tradeRepository = tradeRepository ?? throw new ArgumentNullException(nameof(tradeRepository));
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ilogger = ilogger ?? throw new ArgumentNullException(nameof(ilogger));
        }

        /// <inheritdoc/>
        public async Task UpdateKlineWithTradeAsync(Trade trade)
        {
            try
            {
                _ilogger.LogInformation($"Updating klines for trade {trade.Id} on {trade.Symbol}");

                // Process the trade for all supported intervals
                foreach (var interval in _supportedIntervals)
                {
                    // Calculate the current kline's time range
                    var (startTime, endTime) = CalculateKlineTimeRange(interval, trade.CreatedAt);

                    // Get the current kline, or create if it doesn't exist
                    var kline = await _klineRepository.GetKlineAsync(trade.Symbol, interval, startTime);

                    if (kline == null)
                    {
                        kline = new Kline
                        {
                            Symbol = trade.Symbol,
                            Interval = interval,
                            OpenTime = startTime,
                            CloseTime = endTime,
                            Open = trade.Price,
                            High = trade.Price,
                            Low = trade.Price,
                            Close = trade.Price,
                            Volume = trade.Quantity,
                            QuoteVolume = trade.Price * trade.Quantity,
                            TradeCount = 1
                        };
                    }
                    else
                    {
                        // Update the existing kline
                        kline.High = Math.Max(kline.High, trade.Price);
                        kline.Low = Math.Min(kline.Low, trade.Price);
                        kline.Close = trade.Price;
                        kline.Volume += trade.Quantity;
                        kline.QuoteVolume += trade.Price * trade.Quantity;
                        kline.TradeCount++;
                    }

                    // Save the updated kline
                    await _klineRepository.UpsertKlineAsync(kline);

                    // Publish WebSocket update
                    await _webSocketService.PublishKlineUpdate(trade.Symbol, interval, KlineToKlineData(kline));

                    _ilogger.LogDebug($"Updated {interval} kline for {trade.Symbol}, Open: {kline.Open}, Close: {kline.Close}, Volume: {kline.Volume}");
                }
            }
            catch (Exception ex)
            {
                _ilogger.LogError(ex, $"Error updating klines with trade {trade.Id}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task GenerateKlineAsync(string symbol, string interval, DateTime startTime, DateTime endTime)
        {
            try
            {
                _ilogger.LogInformation($"Generating {interval} kline for {symbol} from {startTime} to {endTime}");

                // Validate the interval
                if (!_supportedIntervals.Contains(interval))
                {
                    throw new ArgumentException($"Unsupported interval: {interval}");
                }

                // Get all trades in the time range
                var trades = await _tradeRepository.GetTradesInTimeRangeAsync(symbol, startTime, endTime);

                if (!trades.Any())
                {
                    _ilogger.LogInformation($"No trades found for {symbol} from {startTime} to {endTime}");
                    return;
                }

                // Create the kline
                var kline = new Kline
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTime = startTime,
                    CloseTime = endTime,
                    Open = trades.First().Price,
                    High = trades.Max(t => t.Price),
                    Low = trades.Min(t => t.Price),
                    Close = trades.Last().Price,
                    Volume = trades.Sum(t => t.Quantity),
                    QuoteVolume = trades.Sum(t => t.Price * t.Quantity),
                    TradeCount = trades.Count
                };

                // Save the kline
                await _klineRepository.UpsertKlineAsync(kline);

                // Publish WebSocket update
                await _webSocketService.PublishKlineUpdate(symbol, interval, KlineToKlineData(kline));

                _ilogger.LogInformation($"Generated {interval} kline for {symbol}, Open: {kline.Open}, Close: {kline.Close}, Volume: {kline.Volume}");
            }
            catch (Exception ex)
            {
                _ilogger.LogError(ex, $"Error generating {interval} kline for {symbol}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RunKlineAggregationTaskAsync(string interval)
        {
            try
            {
                _ilogger.LogInformation($"Running kline aggregation task for {interval} interval");

                // Validate the interval
                if (!_supportedIntervals.Contains(interval))
                {
                    throw new ArgumentException($"Unsupported interval: {interval}");
                }

                // Get all active symbols
                var symbols = await _symbolRepository.GetActiveSymbolsAsync();

                foreach (var symbol in symbols)
                {
                    try
                    {
                        // Calculate the previous interval time range
                        var now = DateTime.UtcNow;
                        var (startTime, endTime) = CalculatePreviousKlineTimeRange(interval, now);

                        // Generate the kline for the previous interval
                        await GenerateKlineAsync(symbol.Name, interval, startTime, endTime);
                    }
                    catch (Exception ex)
                    {
                        _ilogger.LogError(ex, $"Error generating {interval} kline for {symbol.Name}: {ex.Message}");
                        // Continue with other symbols
                    }
                }

                _ilogger.LogInformation($"Completed kline aggregation task for {interval} interval");
            }
            catch (Exception ex)
            {
                _ilogger.LogError(ex, $"Error running kline aggregation task for {interval}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public (DateTime startTime, DateTime endTime) CalculateKlineTimeRange(string interval, DateTime timestamp)
        {
            DateTime startTime;
            DateTime endTime;

            switch (interval)
            {
                case "1m":
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, 0, DateTimeKind.Utc);
                    endTime = startTime.AddMinutes(1).AddMilliseconds(-1);
                    break;

                case "5m":
                    var minute5 = timestamp.Minute - (timestamp.Minute % 5);
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, minute5, 0, DateTimeKind.Utc);
                    endTime = startTime.AddMinutes(5).AddMilliseconds(-1);
                    break;

                case "15m":
                    var minute15 = timestamp.Minute - (timestamp.Minute % 15);
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, minute15, 0, DateTimeKind.Utc);
                    endTime = startTime.AddMinutes(15).AddMilliseconds(-1);
                    break;

                case "30m":
                    var minute30 = timestamp.Minute - (timestamp.Minute % 30);
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, minute30, 0, DateTimeKind.Utc);
                    endTime = startTime.AddMinutes(30).AddMilliseconds(-1);
                    break;

                case "1h":
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc);
                    endTime = startTime.AddHours(1).AddMilliseconds(-1);
                    break;

                case "4h":
                    var hour4 = timestamp.Hour - (timestamp.Hour % 4);
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, hour4, 0, 0, DateTimeKind.Utc);
                    endTime = startTime.AddHours(4).AddMilliseconds(-1);
                    break;

                case "1d":
                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, DateTimeKind.Utc);
                    endTime = startTime.AddDays(1).AddMilliseconds(-1);
                    break;

                case "1w":
                    // Start from Monday
                    int daysToSubtract = (int)timestamp.DayOfWeek - 1;
                    if (daysToSubtract < 0) daysToSubtract += 7; // If Sunday (0), go back 6 days

                    startTime = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-daysToSubtract);
                    endTime = startTime.AddDays(7).AddMilliseconds(-1);
                    break;

                default:
                    throw new ArgumentException($"Unsupported interval: {interval}");
            }

            return (startTime, endTime);
        }

        /// <summary>
        /// Calculates the previous time range for a kline interval
        /// </summary>
        /// <param name="interval">Kline interval (e.g., "1m", "5m", "1h")</param>
        /// <param name="currentTime">Current timestamp</param>
        /// <returns>Tuple with start and end time for the previous interval</returns>
        private (DateTime startTime, DateTime endTime) CalculatePreviousKlineTimeRange(string interval, DateTime currentTime)
        {
            var (currentStartTime, _) = CalculateKlineTimeRange(interval, currentTime);

            DateTime previousStartTime;
            DateTime previousEndTime;

            switch (interval)
            {
                case "1m":
                    previousStartTime = currentStartTime.AddMinutes(-1);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "5m":
                    previousStartTime = currentStartTime.AddMinutes(-5);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "15m":
                    previousStartTime = currentStartTime.AddMinutes(-15);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "30m":
                    previousStartTime = currentStartTime.AddMinutes(-30);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "1h":
                    previousStartTime = currentStartTime.AddHours(-1);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "4h":
                    previousStartTime = currentStartTime.AddHours(-4);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "1d":
                    previousStartTime = currentStartTime.AddDays(-1);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                case "1w":
                    previousStartTime = currentStartTime.AddDays(-7);
                    previousEndTime = currentStartTime.AddMilliseconds(-1);
                    break;

                default:
                    throw new ArgumentException($"Unsupported interval: {interval}");
            }

            return (previousStartTime, previousEndTime);
        }

        /// <inheritdoc/>
        public KlineData KlineToKlineData(Kline kline)
        {
            return new KlineData
            {
                Symbol = kline.Symbol,
                Interval = kline.Interval,
                OpenTime = new DateTimeOffset(kline.OpenTime).ToUnixTimeMilliseconds(),
                CloseTime = new DateTimeOffset(kline.CloseTime).ToUnixTimeMilliseconds(),
                OpenPrice = kline.Open,
                HighPrice = kline.High,
                LowPrice = kline.Low,
                ClosePrice = kline.Close,
                Volume = kline.Volume,
                TradeCount = kline.TradeCount
            };
        }
    }
}