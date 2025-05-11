using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Models.Trading;
using MarketDataService.Repositories;
using MongoDB.Bson;

namespace MarketDataService.Services
{
    /// <summary>
    /// Implementation of IMarketService
    /// </summary>
    public class MarketService : IMarketService
    {
        private readonly ISymbolRepository _symbolRepository;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IOrderBookRepository _orderBookRepository;
        private readonly IKlineRepository _klineRepository;
        private readonly ITradeRepository _tradeRepository;
        private readonly ILogger<MarketService> _logger;

        /// <summary>
        /// Constructor for MarketService
        /// </summary>
        public MarketService(
            ISymbolRepository symbolRepository,
            IMarketDataRepository marketDataRepository,
            IOrderBookRepository orderBookRepository,
            IKlineRepository klineRepository,
            ITradeRepository tradeRepository,
            ILogger<MarketService> logger)
        {
            _symbolRepository = symbolRepository ?? throw new ArgumentNullException(nameof(symbolRepository));
            _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
            _orderBookRepository = orderBookRepository ?? throw new ArgumentNullException(nameof(orderBookRepository));
            _klineRepository = klineRepository ?? throw new ArgumentNullException(nameof(klineRepository));
            _tradeRepository = tradeRepository ?? throw new ArgumentNullException(nameof(tradeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<SymbolsResponse> GetSymbolsAsync()
        {
            _logger.LogInformation("Getting all symbols");
            var symbols = await _symbolRepository.GetAllSymbolsAsync();

            return new SymbolsResponse
            {
                Symbols = symbols.Select(s => new SymbolInfo
                {
                    Symbol = s.Name,
                    BaseAsset = s.BaseAsset,
                    QuoteAsset = s.QuoteAsset,
                    BaseAssetPrecision = s.BaseAssetPrecision,
                    QuotePrecision = s.QuotePrecision,
                    IsActive = s.IsActive
                }).ToList()
            };
        }

        /// <inheritdoc />
        public async Task<TickerResponse> GetTickerAsync(string symbolName)
        {
            _logger.LogInformation($"Getting ticker for symbol: {symbolName}");
            var marketData = await _marketDataRepository.GetMarketDataBySymbolAsync(symbolName);

            if (marketData == null)
            {
                throw new KeyNotFoundException($"Symbol {symbolName} not found or has no market data");
            }

            return new TickerResponse
            {
                Symbol = marketData.Symbol,
                Price = marketData.LastPrice,
                PriceChange = marketData.PriceChange,
                PriceChangePercent = marketData.PriceChangePercent,
                High24h = marketData.High24h,
                Low24h = marketData.Low24h,
                Volume24h = marketData.Volume24h,
                Timestamp = ((DateTimeOffset)marketData.UpdatedAt).ToUnixTimeMilliseconds()
            };
        }

        /// <inheritdoc />
        public async Task<MarketSummaryResponse> GetMarketSummaryAsync()
        {
            _logger.LogInformation("Getting market summary");
            var allMarketData = await _marketDataRepository.GetAllMarketDataAsync();

            return new MarketSummaryResponse
            {
                Markets = allMarketData.Select(md => new TickerResponse
                {
                    Symbol = md.Symbol,
                    Price = md.LastPrice,
                    PriceChange = md.PriceChange,
                    PriceChangePercent = md.PriceChangePercent,
                    High24h = md.High24h,
                    Low24h = md.Low24h,
                    Volume24h = md.Volume24h,
                    Timestamp = ((DateTimeOffset)md.UpdatedAt).ToUnixTimeMilliseconds()
                }).ToList()
            };
        }

        /// <inheritdoc />
        public async Task<MarketDepthResponse> GetMarketDepthAsync(MarketDepthRequest request)
        {
            _logger.LogInformation($"Getting market depth for symbol: {request.Symbol}, limit: {request.Limit}");
            var orderBook = await _orderBookRepository.GetOrderBookBySymbolAsync(request.Symbol);

            if (orderBook == null)
            {
                throw new KeyNotFoundException($"Symbol {request.Symbol} not found or has no order book data");
            }

            // Limit the number of price levels
            int limit = Math.Min(request.Limit, 500);

            return new MarketDepthResponse
            {
                Symbol = orderBook.Symbol,
                Timestamp = ((DateTimeOffset)orderBook.UpdatedAt).ToUnixTimeMilliseconds(),
                Bids = orderBook.Bids.Take(limit).Select(p => new decimal[] { p.Price, p.Quantity }).ToList(),
                Asks = orderBook.Asks.Take(limit).Select(p => new decimal[] { p.Price, p.Quantity }).ToList()
            };
        }

        /// <inheritdoc />
        public async Task<List<decimal[]>> GetKlinesAsync(KlineRequest request)
        {
            _logger.LogInformation($"Getting klines for symbol: {request.Symbol}, interval: {request.Interval}, limit: {request.Limit}");

            DateTime? startTime = request.StartTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(request.StartTime.Value).DateTime : null;
            DateTime? endTime = request.EndTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(request.EndTime.Value).DateTime : null;

            var klines = await _klineRepository.GetKlinesAsync(
                request.Symbol,
                request.Interval,
                startTime,
                endTime,
                request.Limit);

            // Format klines as required by the frontend
            // [openTime, open, high, low, close, volume, closeTime, quoteVolume, trades, isFinal]
            return klines.Select(k => new decimal[]
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
        }

        /// <inheritdoc />
        public async Task<List<CommonLib.Models.Market.TradeResponse>> GetRecentTradesAsync(string symbolName, int limit = 100)
        {
            _logger.LogInformation($"Getting recent trades for symbol: {symbolName}, limit: {limit}");

            var trades = await _tradeRepository.GetRecentTradesAsync(symbolName, limit);

            return trades.Select(t => new CommonLib.Models.Market.TradeResponse
            {
                Id = t.Id.ToString(),
                Symbol = t.Symbol,
                Price = t.Price,
                Quantity = t.Quantity,
                Time = ((DateTimeOffset)t.CreatedAt).ToUnixTimeMilliseconds(),
                IsBuyerMaker = t.IsBuyerMaker
            }).ToList();
        }
    }
}