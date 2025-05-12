using System;
using System.Collections.Generic;
using System.Linq;
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
    [Route("[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IApiLoggingService _apiLogger;
        private readonly ILoggerService _logger;

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
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                // Log API request
                _ = _apiLogger.LogApiRequest(HttpContext);

                var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user identity");
                }

                var account = await _accountService.GetAccountBalanceAsync(userId);

                // Map to API response model
                var response = new BalanceResponse
                {
                    Balances = account.Balances.Select(b => new BalanceInfo
                    {
                        Asset = b.Asset,
                        Free = b.Free,
                        Locked = b.Locked,
                        UpdatedAt = new DateTimeOffset(b.UpdatedAt).ToUnixTimeMilliseconds()
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting balance: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving account balance");
            }
        }

        /// <summary>
        /// Gets transaction history for the authenticated user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="startTime">Start time in Unix timestamp (optional)</param>
        /// <param name="endTime">End time in Unix timestamp (optional)</param>
        /// <param name="type">Transaction type filter (optional)</param>
        /// <returns>Transaction history response</returns>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] long? startTime = null,
            [FromQuery] long? endTime = null,
            [FromQuery] string? type = null)
        {
            try
            {
                _ = _apiLogger.LogApiRequest(HttpContext);

                var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user identity");
                }

                var (transactions, total) = await _accountService.GetTransactionHistoryAsync(
                    userId, page, pageSize, startTime, endTime, type);

                // Map to API response model
                var response = new TransactionListResponse
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting transaction history: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving transaction history");
            }
        }

        /// <summary>
        /// Creates a deposit for the authenticated user
        /// </summary>
        /// <param name="request">Deposit request</param>
        /// <returns>Transaction record</returns>
        [HttpPost("deposit")]
        public async Task<IActionResult> CreateDeposit([FromBody] DepositRequest request)
        {
            try
            {
                _ = _apiLogger.LogApiRequest(HttpContext);

                var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user identity");
                }

                if (request.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }

                var transaction = await _accountService.CreateDepositAsync(
                    userId, request.Asset, request.Amount, request.Reference);

                // Map to API response model
                var response = new DepositResponse
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating deposit: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the deposit");
            }
        }

        /// <summary>
        /// Creates a withdrawal request for the authenticated user
        /// </summary>
        /// <param name="request">Withdrawal request</param>
        /// <returns>Withdrawal response</returns>
        [HttpPost("withdraw")]
        public async Task<IActionResult> CreateWithdrawal([FromBody] WithdrawalRequest request)
        {
            try
            {
                _ = _apiLogger.LogApiRequest(HttpContext);

                var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user identity");
                }

                if (request.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }

                if (string.IsNullOrEmpty(request.Address))
                {
                    return BadRequest("Address is required");
                }

                var withdrawal = await _accountService.CreateWithdrawalAsync(
                    userId, request.Asset, request.Amount, request.Address, request.Memo);

                // Create a WithdrawalResponse model   
                var response = new WithdrawalResponse
                {
                    WithdrawalId = withdrawal.Id.ToString(),
                    Status = withdrawal.Status,
                    EstimatedCompletionTime = new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeMilliseconds()
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Invalid withdrawal request: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating withdrawal: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the withdrawal request");
            }
        }

        /// <summary>
        /// Gets withdrawal request status by ID
        /// </summary>
        /// <param name="id">Withdrawal request ID</param>
        /// <returns>Withdrawal response</returns>
        [HttpGet("withdrawals/{id}")]
        public async Task<IActionResult> GetWithdrawal(string id)
        {
            try
            {
                _ = _apiLogger.LogApiRequest(HttpContext);

                var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user identity");
                }

                var withdrawal = await _accountService.GetWithdrawalByIdAsync(userId, id);

                if (withdrawal == null)
                {
                    return NotFound("Withdrawal request not found");
                }

                // Calculate estimated completion time based on status
                var estimatedCompletionTime = withdrawal.Status == "completed" || withdrawal.Status == "rejected"
                    ? withdrawal.CompletedAt.HasValue
                        ? new DateTimeOffset(withdrawal.CompletedAt.Value).ToUnixTimeMilliseconds()
                        : 0
                    : new DateTimeOffset(withdrawal.CreatedAt.AddHours(1)).ToUnixTimeMilliseconds();

                // Map to API response model
                var response = new WithdrawalResponse
                {
                    WithdrawalId = withdrawal.Id.ToString(),
                    Status = withdrawal.Status,
                    EstimatedCompletionTime = estimatedCompletionTime
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting withdrawal: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving withdrawal information");
            }
        }

        /// <summary>
        /// Gets available assets
        /// </summary>
        /// <returns>List of available assets</returns>
        [HttpGet("assets")]
        public async Task<IActionResult> GetAvailableAssets()
        {
            try
            {
                _ = _apiLogger.LogApiRequest(HttpContext);

                var assets = await _accountService.GetAvailableAssetsAsync();
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting available assets: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving available assets");
            }
        }

        /// <summary>
        /// Creates a new account for a user
        /// </summary>
        /// <param name="request">Account creation request containing user ID and username</param>
        /// <returns>Created account details</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.UserId))
                    return BadRequest(new { message = "User ID is required" });

                if (string.IsNullOrEmpty(request.Username))
                    return BadRequest(new { message = "Username is required" });

                // Check if ObjectId is valid
                if (!ObjectId.TryParse(request.UserId, out var userId))
                    return BadRequest(new { message = "Invalid user ID format" });

                // Get the calling user ID from JWT token
                var callingUserId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(callingUserId))
                {
                    return Unauthorized(new { message = "Invalid authentication token" });
                }

                // Only allow IdentityService or admins to create accounts for other users
                // This is a simplified check - in a real system you might have a service-to-service authentication mechanism
                bool isAdmin = User.IsInRole("Admin");
                bool isIdentityService = User.HasClaim(c => c.Type == "ServiceName" && c.Value == "IdentityService");

                if (!isAdmin && !isIdentityService && callingUserId != request.UserId)
                {
                    _logger.LogWarning($"User {callingUserId} attempted to create account for another user {request.UserId}");
                    return Forbid();
                }

                // Create account
                var account = new Account
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "active",
                    Balances = new List<Balance>()
                };

                var createdAccount = await _accountService.CreateAccountAsync(account);

                // Return response
                var response = new CreateAccountResponse
                {
                    AccountId = createdAccount.Id.ToString(),
                    UserId = createdAccount.UserId.ToString(),
                    Message = "Account created successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating the account" });
            }
        }
    }
}