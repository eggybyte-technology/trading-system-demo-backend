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
        public async Task<OrderBook> GetOrderBookDepthAsync(MarketDepthRequest request)
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

            // 将可空的DateTime?转换为DateTime，如果为null则使用默认值
            var startTimeValue = startTime ?? DateTime.UtcNow.AddDays(-7);
            var endTimeValue = endTime ?? DateTime.UtcNow;

            return await _klineRepository.GetKlinesAsync(
                request.Symbol,
                request.Interval,
                startTimeValue,
                endTimeValue,
                request.Limit);
        }

        /// <inheritdoc />
        public async Task<List<Trade>> GetRecentTradesAsync(RecentTradesRequest request)
        {
            _logger.LogInformation($"Getting recent trades for symbol: {request.Symbol}, limit: {request.Limit}");
            return await _tradeRepository.GetRecentTradesAsync(request.Symbol, request.Limit);
        }

        /// <inheritdoc />
        public async Task<Symbol> CreateSymbolAsync(SymbolCreateRequest request)
        {
            _logger.LogInformation($"Creating new symbol: {request.Name}");

            // Check if symbol already exists
            var existingSymbol = await _symbolRepository.GetSymbolByNameAsync(request.Name);
            if (existingSymbol != null)
            {
                throw new InvalidOperationException($"Symbol {request.Name} already exists");
            }

            // Create new symbol
            var symbol = new Symbol
            {
                Name = request.Name,
                BaseAsset = request.BaseAsset,
                QuoteAsset = request.QuoteAsset,
                BaseAssetPrecision = request.BaseAssetPrecision,
                QuotePrecision = request.QuotePrecision,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                TickSize = request.TickSize,
                MinQty = request.MinQty,
                MaxQty = request.MaxQty,
                StepSize = request.StepSize,
                MinOrderSize = request.MinOrderSize,
                MaxOrderSize = request.MaxOrderSize,
                TakerFee = request.TakerFee,
                MakerFee = request.MakerFee,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save symbol
            var createdSymbol = await _symbolRepository.CreateSymbolAsync(symbol);

            // Initialize order book for the symbol
            await _orderBookRepository.InitOrderBookAsync(request.Name);

            // Initialize market data for the symbol
            await _marketDataRepository.InitMarketDataAsync(request.Name, request.BaseAsset, request.QuoteAsset);

            return createdSymbol;
        }

        /// <inheritdoc />
        public async Task<Symbol> UpdateSymbolAsync(string symbolName, SymbolUpdateRequest request)
        {
            _logger.LogInformation($"Updating symbol: {symbolName}");

            // Get existing symbol
            var symbol = await _symbolRepository.GetSymbolByNameAsync(symbolName);
            if (symbol == null)
            {
                throw new KeyNotFoundException($"Symbol {symbolName} not found");
            }

            // Update fields if provided in request
            if (request.MinPrice.HasValue) symbol.MinPrice = request.MinPrice.Value;
            if (request.MaxPrice.HasValue) symbol.MaxPrice = request.MaxPrice.Value;
            if (request.TickSize.HasValue) symbol.TickSize = request.TickSize.Value;
            if (request.MinQty.HasValue) symbol.MinQty = request.MinQty.Value;
            if (request.MaxQty.HasValue) symbol.MaxQty = request.MaxQty.Value;
            if (request.StepSize.HasValue) symbol.StepSize = request.StepSize.Value;
            if (request.MinOrderSize.HasValue) symbol.MinOrderSize = request.MinOrderSize.Value;
            if (request.MaxOrderSize.HasValue) symbol.MaxOrderSize = request.MaxOrderSize.Value;
            if (request.TakerFee.HasValue) symbol.TakerFee = request.TakerFee.Value;
            if (request.MakerFee.HasValue) symbol.MakerFee = request.MakerFee.Value;
            if (request.IsActive.HasValue) symbol.IsActive = request.IsActive.Value;

            symbol.UpdatedAt = DateTime.UtcNow;

            // Save updated symbol
            return await _symbolRepository.UpdateSymbolAsync(symbol);
        }
    }
}