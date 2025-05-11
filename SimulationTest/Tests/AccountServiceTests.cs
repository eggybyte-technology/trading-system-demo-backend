using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using CommonLib.Models.Account;
using SimulationTest.Core;
using MongoDB.Bson;
using CommonLib.Models;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for the Account Service API
    /// </summary>
    public class AccountServiceTests : ApiTestBase
    {
        /// <summary>
        /// Test connectivity to Account Service before running tests
        /// </summary>
        [ApiTest("Test connectivity to Account Service")]
        public async Task<ApiTestResult> CheckConnectivity_AccountService_ShouldBeAccessible()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Try to connect to the health endpoint
                var client = _httpClientFactory.GetClient("account");
                var response = await client.GetAsync("/health");

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    return ApiTestResult.Passed(stopwatch.Elapsed);
                }
                else
                {
                    return ApiTestResult.Failed(
                        $"Failed to connect to Account Service. Status code: {response.StatusCode}",
                        null,
                        stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception while connecting to Account Service: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that account balances can be retrieved
        /// </summary>
        [ApiTest("Test getting account balances when authenticated", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> GetBalances_WhenAuthenticated_ShouldReturnBalances()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Act
                var balances = await GetAsync<List<Balance>>("account", "/account/balance");

                // Assert
                stopwatch.Stop();

                if (balances == null)
                {
                    return ApiTestResult.Failed(
                        "Balances response is null",
                        null,
                        stopwatch.Elapsed);
                }

                // In a test environment, we might not have any balances yet,
                // so we just check that the response structure is correct
                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test getting transactions when authenticated
        /// </summary>
        [ApiTest("Test getting transactions when authenticated", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> GetTransactions_WhenAuthenticated_ShouldReturnTransactions()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Act
                var transactions = await GetAsync<PaginatedResult<Transaction>>("account", "/account/transactions");

                // Assert
                stopwatch.Stop();

                if (transactions == null)
                {
                    return ApiTestResult.Failed(
                        "Transactions response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (transactions.Items == null)
                {
                    return ApiTestResult.Failed(
                        "Transactions items is null",
                        null,
                        stopwatch.Elapsed);
                }

                // In a test environment, we might not have any transactions yet,
                // so we just check that the response structure is correct
                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that a deposit can be created
        /// </summary>
        [ApiTest("Test creating a deposit when authenticated", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> CreateDeposit_WithValidData_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                var deposit = new
                {
                    Amount = 100,
                    Asset = "USDT",
                    PaymentMethod = "BANK_TRANSFER",
                    Reference = $"TEST-{Guid.NewGuid():N}"
                };

                // Act
                var createdDeposit = await PostAsync<object, Transaction>("account", "/account/deposit", deposit);

                // Assert
                stopwatch.Stop();

                if (createdDeposit == null)
                {
                    return ApiTestResult.Failed(
                        "Created deposit response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (createdDeposit.Amount != deposit.Amount)
                {
                    return ApiTestResult.Failed(
                        $"Deposit amount should be {deposit.Amount}, but was {createdDeposit.Amount}",
                        null,
                        stopwatch.Elapsed);
                }

                if (createdDeposit.Asset != deposit.Asset)
                {
                    return ApiTestResult.Failed(
                        $"Deposit asset should be {deposit.Asset}, but was {createdDeposit.Asset}",
                        null,
                        stopwatch.Elapsed);
                }

                if (createdDeposit.Type != "Deposit")
                {
                    return ApiTestResult.Failed(
                        $"Transaction type should be Deposit, but was {createdDeposit.Type}",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that a withdrawal can be created
        /// </summary>
        [ApiTest("Test creating a withdrawal with valid data")]
        public async Task<ApiTestResult> CreateWithdrawal_WithValidData_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // First create a deposit to have funds
                var deposit = new
                {
                    Asset = "USDT",
                    Amount = 200.0m,
                    PaymentMethod = "BANK_TRANSFER",
                    Reference = $"TEST-{Guid.NewGuid():N}"
                };

                await PostAsync<object, Transaction>("account", "/account/deposit", deposit);

                // Act
                var withdrawal = new
                {
                    Asset = "USDT",
                    Amount = 50.0m,
                    Destination = "BANK_ACCOUNT",
                    BankDetails = new
                    {
                        AccountNumber = "1234567890",
                        BankName = "Test Bank",
                        BeneficiaryName = "Test User"
                    }
                };

                var withdrawalRequest = await PostAsync<object, WithdrawalRequest>("account", "/account/withdraw", withdrawal);

                // Assert
                stopwatch.Stop();

                if (withdrawalRequest == null)
                {
                    return ApiTestResult.Failed(
                        "Withdrawal request response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (withdrawalRequest.Asset != "USDT")
                {
                    return ApiTestResult.Failed(
                        $"Withdrawal asset should be USDT, but was {withdrawalRequest.Asset}",
                        null,
                        stopwatch.Elapsed);
                }

                if (withdrawalRequest.Amount != 50.0m)
                {
                    return ApiTestResult.Failed(
                        $"Withdrawal amount should be 50.0, but was {withdrawalRequest.Amount}",
                        null,
                        stopwatch.Elapsed);
                }

                if (withdrawalRequest.Status == null)
                {
                    return ApiTestResult.Failed(
                        "Withdrawal status should not be null",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that withdrawal status can be retrieved
        /// </summary>
        [ApiTest("Test getting withdrawal status with valid ID")]
        public async Task<ApiTestResult> GetWithdrawalStatus_WithValidId_ShouldReturnWithdrawal()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // First create a deposit to have funds
                var deposit = new
                {
                    Asset = "USDT",
                    Amount = 200.0m,
                    PaymentMethod = "BANK_TRANSFER",
                    Reference = $"TEST-{Guid.NewGuid():N}"
                };

                await PostAsync<object, Transaction>("account", "/account/deposit", deposit);

                // Then create a withdrawal
                var withdrawal = new
                {
                    Asset = "USDT",
                    Amount = 50.0m,
                    Destination = "BANK_ACCOUNT",
                    BankDetails = new
                    {
                        AccountNumber = "1234567890",
                        BankName = "Test Bank",
                        BeneficiaryName = "Test User"
                    }
                };

                var withdrawalRequest = await PostAsync<object, WithdrawalRequest>("account", "/account/withdraw", withdrawal);

                // Act
                var retrievedWithdrawal = await GetAsync<WithdrawalRequest>("account", $"/account/withdrawals/{withdrawalRequest!.Id}");

                // Assert
                stopwatch.Stop();

                if (retrievedWithdrawal == null)
                {
                    return ApiTestResult.Failed(
                        "Retrieved withdrawal response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (retrievedWithdrawal.Id != withdrawalRequest.Id)
                {
                    return ApiTestResult.Failed(
                        $"Withdrawal ID should be {withdrawalRequest.Id}, but was {retrievedWithdrawal.Id}",
                        null,
                        stopwatch.Elapsed);
                }

                if (retrievedWithdrawal.Asset != "USDT")
                {
                    return ApiTestResult.Failed(
                        $"Withdrawal asset should be USDT, but was {retrievedWithdrawal.Asset}",
                        null,
                        stopwatch.Elapsed);
                }

                if (retrievedWithdrawal.Amount != 50.0m)
                {
                    return ApiTestResult.Failed(
                        $"Withdrawal amount should be 50.0, but was {retrievedWithdrawal.Amount}",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that assets can be retrieved
        /// </summary>
        [ApiTest("Test getting assets when authenticated", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> GetAssets_WhenAuthenticated_ShouldReturnAssets()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Act
                var assets = await GetAsync<List<AssetInfo>>("account", "/account/assets");

                // Assert
                stopwatch.Stop();

                if (assets == null)
                {
                    return ApiTestResult.Failed(
                        "Assets response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (assets.Count == 0)
                {
                    return ApiTestResult.Failed(
                        "Assets list is empty",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }
    }

    /// <summary>
    /// Information about an asset
    /// </summary>
    public class AssetInfo
    {
        /// <summary>
        /// Gets or sets the asset symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets or sets the asset name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the asset is active
        /// </summary>
        public bool IsActive { get; set; }
    }
}