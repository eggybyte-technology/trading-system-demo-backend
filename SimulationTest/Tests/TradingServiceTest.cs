using System.Diagnostics;
using CommonLib.Models;
using CommonLib.Models.Trading;
using SimulationTest.Core;
using CommonLib.Api;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Trading Service
    /// </summary>
    public class TradingServiceTest
    {
        private readonly CommonLib.Api.TradingService _tradingService;
        private readonly TestLogger _logger;
        private readonly StatusBar _statusBar;
        private readonly List<OperationResult> _results = new();
        private readonly TestContext _context;

        public TradingServiceTest(
            CommonLib.Api.TradingService tradingService,
            TestLogger logger,
            StatusBar statusBar,
            TestContext context)
        {
            _tradingService = tradingService;
            _logger = logger;
            _statusBar = statusBar;
            _context = context;
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        public async Task<OrderResponse> TestCreateOrderAsync()
        {
            string operationType = "TradingService.CreateOrderAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var orderRequest = new CreateOrderRequest
                {
                    Symbol = _context.TestSymbol,
                    Side = OrderSide.BUY.ToString(),
                    Type = OrderType.LIMIT.ToString(),
                    Price = 1000,
                    Quantity = 0.1m,
                    TimeInForce = TimeInForce.GTC.ToString()
                };

                var result = await _tradingService.CreateOrderAsync(_context.Token, orderRequest);

                // Store order ID for later use
                _context.OrderId = result.OrderId;

                // Verify response
                if (result == null)
                    throw new AssertionException("Create order response should not be null");
                if (string.IsNullOrEmpty(result.OrderId))
                    throw new AssertionException("Order ID should not be empty");

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
        /// Get order details
        /// </summary>
        public async Task<OrderResponse> TestGetOrderDetailsAsync()
        {
            string operationType = "TradingService.GetOrderDetailsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _tradingService.GetOrderDetailsAsync(_context.Token, _context.OrderId);

                // Verify response
                if (result == null)
                    throw new AssertionException("Order details response should not be null");
                if (result.OrderId != _context.OrderId)
                    throw new AssertionException($"Order ID should match. Expected: {_context.OrderId}, Got: {result.OrderId}");

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
        /// Get open orders
        /// </summary>
        public async Task<OpenOrdersResponse> TestGetOpenOrdersAsync()
        {
            string operationType = "TradingService.GetOpenOrdersAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _tradingService.GetOpenOrdersAsync(_context.Token);

                // Verify response
                if (result == null)
                    throw new AssertionException("Open orders response should not be null");

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
        /// Cancel order
        /// </summary>
        public async Task<CancelOrderResponse> TestCancelOrderAsync()
        {
            string operationType = "TradingService.CancelOrderAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _tradingService.CancelOrderAsync(_context.Token, _context.OrderId);

                // Verify response
                if (result == null)
                    throw new AssertionException("Cancel order response should not be null");

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
        /// Get order history
        /// </summary>
        public async Task<OrderHistoryResponse> TestGetOrderHistoryAsync()
        {
            string operationType = "TradingService.GetOrderHistoryAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var historyRequest = new OrderHistoryRequest
                {
                    Symbol = _context.TestSymbol,
                    Page = 1,
                    PageSize = 10
                };

                var result = await _tradingService.GetOrderHistoryAsync(_context.Token, historyRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Order history response should not be null");
                if (result.Orders == null)
                    throw new AssertionException("Order history items should not be null");

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
        /// Get trade history
        /// </summary>
        public async Task<TradeHistoryResponse> TestGetTradeHistoryAsync()
        {
            string operationType = "TradingService.GetTradeHistoryAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var historyRequest = new TradeHistoryRequest
                {
                    Symbol = _context.TestSymbol,
                    Page = 1,
                    PageSize = 10
                };

                var result = await _tradingService.GetTradeHistoryAsync(_context.Token, historyRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Trade history response should not be null");
                if (result.Items == null)
                    throw new AssertionException("Trade history items should not be null");

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
        /// Test locking an order for processing
        /// </summary>
        public async Task<LockOrderResponse> TestLockOrderAsync()
        {
            string operationType = "TradingService.LockOrderAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var lockRequest = new LockOrderRequest
                {
                    OrderId = _context.OrderId,
                    LockId = Guid.NewGuid().ToString(),
                    TimeoutSeconds = 5
                };

                var result = await _tradingService.LockOrderAsync(_context.AdminToken, lockRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Lock order response should not be null");
                if (result.Order == null)
                    throw new AssertionException("Locked order details should not be null");
                if (string.IsNullOrEmpty(result.LockId))
                    throw new AssertionException("Lock ID should not be empty");

                // Store lock ID for unlock test
                _context.OrderLockId = result.LockId;

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
        /// Test unlocking an order
        /// </summary>
        public async Task<UnlockOrderResponse> TestUnlockOrderAsync()
        {
            string operationType = "TradingService.UnlockOrderAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var unlockRequest = new UnlockOrderRequest
                {
                    OrderId = _context.OrderId,
                    LockId = _context.OrderLockId
                };

                var result = await _tradingService.UnlockOrderAsync(_context.AdminToken, unlockRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Unlock order response should not be null");
                if (!result.Success)
                    throw new AssertionException("Unlock operation should be successful");

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
        /// Test updating order status
        /// </summary>
        public async Task<OrderResponse> TestUpdateOrderStatusAsync()
        {
            string operationType = "TradingService.UpdateOrderStatusAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // First lock the order for update
                var lockRequest = new LockOrderRequest
                {
                    OrderId = _context.OrderId,
                    LockId = Guid.NewGuid().ToString(),
                    TimeoutSeconds = 5
                };

                var lockResult = await _tradingService.LockOrderAsync(_context.AdminToken, lockRequest);

                // Now update the order status
                var updateRequest = new UpdateOrderStatusRequest
                {
                    OrderId = _context.OrderId,
                    Status = "PARTIALLY_FILLED",
                    ExecutedQuantity = 0.05m,
                    CumulativeQuoteQuantity = 50m,
                    LockId = lockResult.LockId
                };

                var result = await _tradingService.UpdateOrderStatusAsync(_context.AdminToken, updateRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Update order status response should not be null");
                if (result.OrderId != _context.OrderId)
                    throw new AssertionException($"Order ID should match. Expected: {_context.OrderId}, Got: {result.OrderId}");
                if (result.Status != updateRequest.Status)
                    throw new AssertionException($"Order status should match. Expected: {updateRequest.Status}, Got: {result.Status}");

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