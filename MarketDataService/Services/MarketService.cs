using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Models.Trading;
using MarketDataService.Repositories;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

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
        public async Task<List<Symbol>> GetSymbolsAsync()
        {
            _logger.LogInformation("Getting all symbols");
            return await _symbolRepository.GetAllSymbolsAsync();
        }

        /// <inheritdoc />
        public async Task<MarketData> GetTickerAsync(string symbolName)
        {
            _logger.LogInformation($"Getting ticker for symbol: {symbolName}");
            var marketData = await _marketDataRepository.GetMarketDataBySymbolAsync(symbolName);

            if (marketData == null)
            {
                throw new KeyNotFoundException($"Symbol {symbolName} not found or has no market data");
            }

            return marketData;
        }

        /// <inheritdoc />
        public async Task<List<MarketData>> GetMarketSummaryAsync()
        {
            _logger.LogInformation("Getting market summary");
            return await _marketDataRepository.GetAllMarketDataAsync();
        }

        /// <inheritdoc />
        public async Task<OrderBook> GetMarketDepthAsync(MarketDepthRequest request)
        {
            _logger.LogInformation($"Getting market depth for symbol: {request.Symbol}, limit: {request.Limit}");
            var orderBook = await _orderBookRepository.GetOrderBookBySymbolAsync(request.Symbol);

            if (orderBook == null)
            {
                throw new KeyNotFoundException($"Symbol {request.Symbol} not found or has no order book data");
            }

            return orderBook;
        }

        /// <inheritdoc />
        public async Task<List<Kline>> GetKlinesAsync(KlineRequest request)
        {
            _logger.LogInformation($"Getting klines for symbol: {request.Symbol}, interval: {request.Interval}, limit: {request.Limit}");

            DateTime? startTime = request.StartTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(request.StartTime.Value).DateTime : null;
            DateTime? endTime = request.EndTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(request.EndTime.Value).DateTime : null;

            return await _klineRepository.GetKlinesAsync(
                request.Symbol,
                request.Interval,
                startTime,
                endTime,
                request.Limit);
        }

        /// <inheritdoc />
        public async Task<List<Trade>> GetRecentTradesAsync(string symbolName, int limit = 100)
        {
            _logger.LogInformation($"Getting recent trades for symbol: {symbolName}, limit: {limit}");
            return await _tradeRepository.GetRecentTradesAsync(symbolName, limit);
        }
    }
}