using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AccountService.Services;
using CommonLib.Models.Account;
using CommonLib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace AccountService.Controllers
{
    /// <summary>
    /// Controller for account operations
    /// </summary>
    [ApiController]
    [Route("account")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IApiLoggingService _apiLogger;
        private readonly ILoggerService _logger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Initializes a new instance of the account controller
        /// </summary>
        /// <param name="accountService">The account service</param>
        /// <param name="apiLogger">The API logging service</param>
        /// <param name="logger">The logger service</param>
        public AccountController(
            IAccountService accountService,
            IApiLoggingService apiLogger,
            ILoggerService logger)
        {
            _accountService = accountService;
            _apiLogger = apiLogger;
            _logger = logger;
        }

        /// <summary>
        /// Gets account balance for the authenticated user
        /// </summary>
        /// <returns>Account balance response</returns>
        [HttpGet("balance")]
        [ProducesResponseType(typeof(BalanceResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBalance()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var account = await _accountService.GetAccountBalanceAsync(userIdClaim);

                // Map to API response model
                var balanceResponse = new BalanceResponse
                {
                    Balances = account.Balances.Select(b => new BalanceInfo
                    {
                        Asset = b.Asset,
                        Free = b.Free,
                        Locked = b.Locked,
                        UpdatedAt = new DateTimeOffset(b.UpdatedAt).ToUnixTimeMilliseconds()
                    }).ToList()
                };

                var response = new { data = balanceResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting balance: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving account balance", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets transaction history for the authenticated user
        /// </summary>
        /// <param name="request">Transaction list request parameters</param>
        /// <returns>Transaction history response</returns>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(TransactionListResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTransactions([FromQuery] TransactionListRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var (transactions, total) = await _accountService.GetTransactionHistoryAsync(
                    userIdClaim, request.Page, request.PageSize, request.StartTime, request.EndTime, request.Type);

                // Map to API response model
                var transactionResponse = new TransactionListResponse
                {
                    Total = total,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Items = transactions.Select(t => new TransactionItem
                    {
                        Id = t.Id.ToString(),
                        UserId = t.UserId.ToString(),
                        Asset = t.Asset,
                        Amount = t.Amount,
                        Type = t.Type,
                        Status = t.Status,
                        Timestamp = new DateTimeOffset(t.CreatedAt).ToUnixTimeMilliseconds(),
                        Reference = t.Reference
                    }).ToList()
                };

                var response = new { data = transactionResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting transaction history: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving transaction history", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Creates a deposit for the authenticated user
        /// </summary>
        /// <param name="request">Deposit request</param>
        /// <returns>Transaction record</returns>
        [HttpPost("deposit")]
        [ProducesResponseType(typeof(DepositResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateDeposit([FromBody] DepositRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                if (request.Amount <= 0)
                {
                    var errorResponse = new { message = "Amount must be greater than zero", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                var transaction = await _accountService.CreateDepositAsync(
                    userIdClaim, request.Asset, request.Amount, request.Reference);

                // Map to API response model
                var depositResponse = new DepositResponse
                {
                    Id = transaction.Id.ToString(),
                    UserId = transaction.UserId.ToString(),
                    Asset = transaction.Asset,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    Timestamp = new DateTimeOffset(transaction.CreatedAt).ToUnixTimeMilliseconds(),
                    Reference = transaction.Reference
                };

                var response = new { data = depositResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating deposit: {ex.Message}");
                var errorResponse = new { message = "An error occurred while processing the deposit", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Creates a withdrawal request for the authenticated user
        /// </summary>
        /// <param name="request">Withdrawal request</param>
        /// <returns>Withdrawal response</returns>
        [HttpPost("withdraw")]
        [ProducesResponseType(typeof(WithdrawalResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateWithdrawal([FromBody] WithdrawalRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                if (request.Amount <= 0)
                {
                    var errorResponse = new { message = "Amount must be greater than zero", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                var withdrawal = await _accountService.CreateWithdrawalAsync(
                    userIdClaim, request.Asset, request.Amount, request.Address, request.Memo);

                // Map to API response model
                var withdrawalResponse = new WithdrawalResponse
                {
                    Id = withdrawal.Id.ToString(),
                    UserId = withdrawal.UserId.ToString(),
                    Asset = withdrawal.Asset,
                    Amount = withdrawal.Amount,
                    Address = withdrawal.Address,
                    Status = withdrawal.Status,
                    Timestamp = new DateTimeOffset(withdrawal.CreatedAt).ToUnixTimeMilliseconds(),
                    TransactionId = withdrawal.TransactionId
                };

                var response = new { data = withdrawalResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (InsufficientFundsException ex)
            {
                _logger.LogWarning($"Insufficient funds for withdrawal: {ex.Message}");
                var errorResponse = new { message = "Insufficient funds for withdrawal", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating withdrawal: {ex.Message}");
                var errorResponse = new { message = "An error occurred while processing the withdrawal", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets withdrawal details by ID
        /// </summary>
        /// <param name="id">Withdrawal ID</param>
        /// <returns>Withdrawal details</returns>
        [HttpGet("withdrawals/{id}")]
        [ProducesResponseType(typeof(WithdrawalResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetWithdrawal(string id)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var withdrawal = await _accountService.GetWithdrawalByIdAsync(userIdClaim, id);

                if (withdrawal == null)
                {
                    var errorResponse = new { message = "Withdrawal not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Map to API response model
                var withdrawalResponse = new WithdrawalResponse
                {
                    Id = withdrawal.Id.ToString(),
                    UserId = withdrawal.UserId.ToString(),
                    Asset = withdrawal.Asset,
                    Amount = withdrawal.Amount,
                    Address = withdrawal.Address,
                    Status = withdrawal.Status,
                    Timestamp = new DateTimeOffset(withdrawal.CreatedAt).ToUnixTimeMilliseconds(),
                    TransactionId = withdrawal.TransactionId
                };

                var response = new { data = withdrawalResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving withdrawal: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving the withdrawal", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets available assets
        /// </summary>
        /// <returns>List of available assets</returns>
        [HttpGet("assets")]
        [ProducesResponseType(typeof(AssetListResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAvailableAssets()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                var assetList = await _accountService.GetAvailableAssetsAsync();

                // Map to API response model
                var assetListResponse = new AssetListResponse
                {
                    Assets = assetList.Select(asset => new AssetInfo
                    {
                        Symbol = asset,
                        Name = asset,
                        IsActive = true,
                        CanDeposit = true,
                        CanWithdraw = true,
                        MinDepositAmount = 0.001m,
                        MinWithdrawalAmount = 0.001m,
                        WithdrawalFeeFixed = 0.0001m,
                        WithdrawalFeePercentage = 0.001m
                    }).ToList()
                };

                var response = new { data = assetListResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving available assets: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving available assets", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Creates a new account (service-to-service call)
        /// </summary>
        /// <param name="request">Create account request</param>
        /// <returns>Created account details</returns>
        [HttpPost("create")]
        [Authorize(Roles = "Service")]
        [ProducesResponseType(typeof(CreateAccountResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Username))
                {
                    var errorResponse = new { message = "UserId and Username are required", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Convert string ID to ObjectId
                if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                {
                    var errorResponse = new { message = "Invalid user ID format", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Create new account
                var account = new Account
                {
                    UserId = userObjectId,
                    Username = request.Username,
                    Balances = new List<Balance>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdAccount = await _accountService.CreateAccountAsync(account);

                // Map to API response model
                var createAccountResponse = new CreateAccountResponse
                {
                    AccountId = createdAccount.Id.ToString(),
                    UserId = createdAccount.UserId.ToString(),
                    Username = createdAccount.Username,
                    CreatedAt = new DateTimeOffset(createdAccount.CreatedAt).ToUnixTimeMilliseconds(),
                    Message = "Account created successfully"
                };

                var response = new { data = createAccountResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                var errorResponse = new { message = "An error occurred while creating the account", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Lock balance for trade execution
        /// </summary>
        [Authorize(Roles = "Admin,Service")]
        [HttpPost("lock-balance")]
        public async Task<IActionResult> LockBalance([FromBody] LockBalanceRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _accountService.LockBalanceAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error locking balance: {ex.Message}");

                var errorResponse = new { message = "Failed to lock balance", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Unlock previously locked balance
        /// </summary>
        [Authorize(Roles = "Admin,Service")]
        [HttpPost("unlock-balance")]
        public async Task<IActionResult> UnlockBalance([FromBody] UnlockBalanceRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _accountService.UnlockBalanceAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unlocking balance: {ex.Message}");

                var errorResponse = new { message = "Failed to unlock balance", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Execute trade with locked balances
        /// </summary>
        [Authorize(Roles = "Admin,Service")]
        [HttpPost("execute-trade")]
        public async Task<IActionResult> ExecuteTrade([FromBody] ExecuteTradeRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _accountService.ExecuteTradeAsync(request);
                var responseObject = new { data = result, success = true };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(responseObject, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(responseObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing trade: {ex.Message}");

                var errorResponse = new { message = "Failed to execute trade", success = false };
                await _apiLogger.LogApiResponse(HttpContext, JsonSerializer.Serialize(errorResponse, _jsonOptions), (long)(DateTime.UtcNow - startTime).TotalMilliseconds);

                return StatusCode(500, errorResponse);
            }
        }
    }
}