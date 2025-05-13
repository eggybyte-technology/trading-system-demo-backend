using System.Diagnostics;
using CommonLib.Models.Account;
using SimulationTest.Core;
using CommonLib.Api;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Account Service
    /// </summary>
    public class AccountServiceTest
    {
        private readonly CommonLib.Api.AccountService _accountService;
        private readonly TestLogger _logger;
        private readonly StatusBar _statusBar;
        private readonly List<OperationResult> _results = new();
        private readonly TestContext _context;

        public AccountServiceTest(
            CommonLib.Api.AccountService accountService,
            TestLogger logger,
            StatusBar statusBar,
            TestContext context)
        {
            _accountService = accountService;
            _logger = logger;
            _statusBar = statusBar;
            _context = context;
        }

        /// <summary>
        /// Get account balance
        /// </summary>
        public async Task<BalanceResponse> TestGetBalanceAsync()
        {
            string operationType = "AccountService.GetBalanceAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _accountService.GetBalanceAsync(_context.Token);

                // Verify response
                if (result == null)
                    throw new AssertionException("Balance response should not be null");
                if (result.Balances == null)
                    throw new AssertionException("Balances list should not be null");

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
        /// Create deposit request
        /// </summary>
        public async Task<DepositResponse> TestCreateDepositAsync()
        {
            string operationType = "AccountService.CreateDepositAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var depositRequest = new DepositRequest
                {
                    Asset = "USDT",
                    Amount = 1000,
                    Reference = "Test deposit"
                };

                var result = await _accountService.CreateDepositAsync(_context.Token, depositRequest);

                // Store deposit ID for later verification
                _context.DepositId = result.Id;

                // Verify response
                if (result == null)
                    throw new AssertionException("Deposit response should not be null");
                if (string.IsNullOrEmpty(result.Id))
                    throw new AssertionException("Deposit ID should not be empty");

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
        /// Create withdrawal request
        /// </summary>
        public async Task<WithdrawalResponse> TestCreateWithdrawalAsync()
        {
            string operationType = "AccountService.CreateWithdrawalAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var withdrawalRequest = new WithdrawalRequest
                {
                    Asset = "USDT",
                    Amount = 100,
                    Address = "0x1234567890abcdef1234567890abcdef12345678",
                    Memo = "Test withdrawal"
                };

                var result = await _accountService.CreateWithdrawalAsync(_context.Token, withdrawalRequest);

                // Store withdrawal ID for later verification
                _context.WithdrawalId = result.Id;

                // Verify response
                if (result == null)
                    throw new AssertionException("Withdrawal response should not be null");
                if (string.IsNullOrEmpty(result.Id))
                    throw new AssertionException("Withdrawal ID should not be empty");

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
        /// Get withdrawal status
        /// </summary>
        public async Task<WithdrawalResponse> TestGetWithdrawalStatusAsync()
        {
            string operationType = "AccountService.GetWithdrawalStatusAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(_context.WithdrawalId))
                    throw new AssertionException("Must create a withdrawal first");

                var result = await _accountService.GetWithdrawalStatusAsync(_context.Token, _context.WithdrawalId);

                // Verify response
                if (result == null)
                    throw new AssertionException("Withdrawal response should not be null");
                if (result.Id != _context.WithdrawalId)
                    throw new AssertionException($"Withdrawal ID mismatch. Expected: {_context.WithdrawalId}, Got: {result.Id}");

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
        /// Get transaction history
        /// </summary>
        public async Task<TransactionListResponse> TestGetTransactionsAsync()
        {
            string operationType = "AccountService.GetTransactionsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _accountService.GetTransactionsAsync(_context.Token, type: "All");

                // Verify response
                if (result == null)
                    throw new AssertionException("Transaction list response should not be null");
                if (result.Items == null)
                    throw new AssertionException("Transaction items should not be null");

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
        /// Get available assets
        /// </summary>
        public async Task<AssetListResponse> TestGetAssetsAsync()
        {
            string operationType = "AccountService.GetAssetsAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _accountService.GetAssetsAsync(_context.Token);

                // Verify response
                if (result == null)
                    throw new AssertionException("Asset list response should not be null");

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
                Timestamp = DateTime.UtcNow
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
                Timestamp = DateTime.UtcNow,
                ErrorMessage = errorMessage
            });

            _statusBar.ReportFailure();
        }

        public List<OperationResult> GetResults() => _results;
    }
}