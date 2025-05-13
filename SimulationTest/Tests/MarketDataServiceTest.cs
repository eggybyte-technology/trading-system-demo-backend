using System.Diagnostics;
using CommonLib.Models.Market;
using SimulationTest.Core;
using CommonLib.Api;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Market Data Service
    /// </summary>
    public class MarketDataServiceTest
    {
        private readonly CommonLib.Api.MarketDataService _marketDataService;
        private readonly TestLogger _logger;
        private readonly StatusBar _statusBar;
        private readonly List<OperationResult> _results = new();
        private readonly TestContext _context;

        public MarketDataServiceTest(
            CommonLib.Api.MarketDataService marketDataService,
            TestLogger logger,
            StatusBar statusBar,
            TestContext context)
        {
            _marketDataService = marketDataService;
            _logger = logger;
            _statusBar = statusBar;
            _context = context;
        }

        /// <summary>
        /// Get all trading symbols
        /// </summary>
        public async Task<SymbolsResponse> TestGetSymbolsAsync()
        {
            string operationType = "MarketDataService.GetSymbolsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _marketDataService.GetSymbolsAsync();

                // Store symbols for later use
                if (result.Symbols != null && result.Symbols.Count > 0)
                {
                    var activeSymbols = result.Symbols.Where(s => s.IsActive).ToList();
                    if (activeSymbols.Count > 0)
                    {
                        _context.TestSymbol = activeSymbols[0].Symbol;
                    }
                }

                // Verify response
                if (result == null)
                    throw new AssertionException("Symbols response should not be null");
                if (result.Symbols == null)
                    throw new AssertionException("Symbols list should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get ticker for a symbol
        /// </summary>
        public async Task<TickerResponse> TestGetTickerAsync()
        {
            string operationType = "MarketDataService.GetTickerAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _marketDataService.GetTickerAsync(_context.TestSymbol);

                // Verify response
                if (result == null)
                    throw new AssertionException("Ticker response should not be null");
                if (result.Symbol != _context.TestSymbol)
                    throw new AssertionException($"Symbol should match the requested one. Expected: {_context.TestSymbol}, Got: {result.Symbol}");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get market summary
        /// </summary>
        public async Task<MarketSummaryResponse> TestGetMarketSummaryAsync()
        {
            string operationType = "MarketDataService.GetMarketSummaryAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _marketDataService.GetMarketSummaryAsync();

                // Verify response
                if (result == null)
                    throw new AssertionException("Market summary response should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get order book depth
        /// </summary>
        public async Task<MarketDepthResponse> TestGetOrderBookDepthAsync()
        {
            string operationType = "MarketDataService.GetOrderBookDepthAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var depthRequest = new MarketDepthRequest
                {
                    Symbol = _context.TestSymbol,
                    Limit = 10
                };

                var result = await _marketDataService.GetOrderBookDepthAsync(depthRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Order book depth response should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get klines (candlestick data)
        /// </summary>
        public async Task<KlineResponse> TestGetKlinesAsync()
        {
            string operationType = "MarketDataService.GetKlinesAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var klineRequest = new KlineRequest
                {
                    Symbol = _context.TestSymbol,
                    Interval = "1h",
                    Limit = 10
                };

                var result = await _marketDataService.GetKlinesAsync(klineRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Klines response should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get recent trades
        /// </summary>
        public async Task<TradesResponse> TestGetRecentTradesAsync()
        {
            string operationType = "MarketDataService.GetRecentTradesAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var tradesRequest = new RecentTradesRequest
                {
                    Symbol = _context.TestSymbol,
                    Limit = 10
                };

                var result = await _marketDataService.GetRecentTradesAsync(tradesRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Recent trades response should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test updating order book
        /// </summary>
        public async Task<OrderBookUpdateResponse> TestUpdateOrderBookAsync()
        {
            string operationType = "MarketDataService.UpdateOrderBookAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var request = new OrderBookUpdateRequest
                {
                    Symbol = _context.TestSymbol,
                    Bids = new List<List<decimal>>
                    {
                        new List<decimal> { 1000.0m, 1.0m },
                        new List<decimal> { 990.0m, 2.0m },
                    },
                    Asks = new List<List<decimal>>
                    {
                        new List<decimal> { 1010.0m, 1.5m },
                        new List<decimal> { 1020.0m, 2.5m },
                    }
                };

                var result = await _marketDataService.UpdateOrderBookAsync(_context.AdminToken, request);

                // Verify response
                if (result == null)
                    throw new AssertionException("Order book update response should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test creating a new trading symbol
        /// </summary>
        public async Task<SymbolResponse> TestCreateSymbolAsync()
        {
            string operationType = "MarketDataService.CreateSymbolAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                string newSymbol = $"TEST-USDT-{DateTime.Now.Ticks % 10000}";
                var request = new SymbolCreateRequest
                {
                    Name = newSymbol,
                    BaseAsset = "TEST",
                    QuoteAsset = "USDT",
                    MinOrderSize = 0.0001m,
                    MaxOrderSize = 1000m,
                    BaseAssetPrecision = 8,
                    QuotePrecision = 2,
                    MinPrice = 0.01m,
                    MaxPrice = 100000m,
                    TickSize = 0.01m,
                    MinQty = 0.0001m,
                    MaxQty = 10000m,
                    StepSize = 0.0001m,
                    IsActive = true
                };

                var result = await _marketDataService.CreateSymbolAsync(_context.AdminToken, request);

                // Verify response
                if (result == null)
                    throw new AssertionException("Symbol creation response should not be null");
                if (!result.Success)
                    throw new AssertionException($"Symbol creation failed: {result.Message}");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test updating a trading symbol
        /// </summary>
        public async Task<SymbolResponse> TestUpdateSymbolAsync()
        {
            string operationType = "MarketDataService.UpdateSymbolAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var request = new SymbolUpdateRequest
                {
                    MinOrderSize = 0.001m,
                    MaxOrderSize = 500m,
                    IsActive = true
                };

                var result = await _marketDataService.UpdateSymbolAsync(_context.AdminToken, _context.TestSymbol, request);

                // Verify response
                if (result == null)
                    throw new AssertionException("Symbol update response should not be null");
                if (!result.Success)
                    throw new AssertionException($"Symbol update failed: {result.Message}");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test processing a trade for kline generation
        /// </summary>
        public async Task<ApiResponse<bool>> TestProcessTradeForKlineAsync()
        {
            string operationType = "MarketDataService.ProcessTradeForKlineAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var request = new TradeForKlineRequest
                {
                    Symbol = _context.TestSymbol,
                    Price = 1000.0m,
                    Quantity = 1.0m,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    IsBuyerMaker = false
                };

                var result = await _marketDataService.ProcessTradeForKlineAsync(_context.AdminToken, request);

                // Verify response
                if (result == null)
                    throw new AssertionException("Process trade for kline response should not be null");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        private void ReportSuccess(string operationType, long latencyMs)
        {
            _results.Add(new OperationResult
            {
                OperationType = operationType,
                UserId = _context.UserId,
                Success = true,
                LatencyMs = latencyMs,
                Timestamp = DateTime.Now
            });

            _statusBar.ReportSuccess(latencyMs);
        }

        private void ReportFailure(string operationType, string errorMessage, long latencyMs)
        {
            _results.Add(new OperationResult
            {
                OperationType = operationType,
                UserId = _context.UserId,
                Success = false,
                LatencyMs = latencyMs,
                Timestamp = DateTime.Now,
                ErrorMessage = errorMessage
            });

            _statusBar.ReportFailure();
        }

        public List<OperationResult> GetResults() => _results;
    }
}