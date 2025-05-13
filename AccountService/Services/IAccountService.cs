using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Account;
using MongoDB.Bson;

namespace AccountService.Services
{
    /// <summary>
    /// Interface for account management service operations
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Gets an account by user ID
        /// </summary>
        /// <param name="userId">The user ID as ObjectId</param>
        /// <returns>Account if found, null otherwise</returns>
        Task<Account> GetAccountByUserIdAsync(ObjectId userId);

        /// <summary>
        /// Creates a new account for a user
        /// </summary>
        /// <param name="account">The account to create</param>
        /// <returns>The created account</returns>
        Task<Account> CreateAccountAsync(Account account);

        /// <summary>
        /// Gets account balance for a user
        /// </summary>
        /// <param name="userId">The user ID (as string)</param>
        /// <returns>Account with balances</returns>
        Task<Account> GetAccountBalanceAsync(string userId);

        /// <summary>
        /// Gets transaction history for a user with pagination
        /// </summary>
        /// <param name="userId">The user ID (as string)</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="startTime">Optional start time in Unix timestamp milliseconds</param>
        /// <param name="endTime">Optional end time in Unix timestamp milliseconds</param>
        /// <param name="type">Optional transaction type filter</param>
        /// <returns>Tuple with transactions list and total count</returns>
        Task<(List<Transaction> Transactions, long Total)> GetTransactionHistoryAsync(
            string userId,
            int page,
            int pageSize,
            long? startTime = null,
            long? endTime = null,
            string? type = null);

        /// <summary>
        /// Creates a deposit transaction
        /// </summary>
        /// <param name="userId">The user ID (as string)</param>
        /// <param name="asset">Asset type (e.g., BTC, ETH)</param>
        /// <param name="amount">Deposit amount</param>
        /// <param name="reference">Optional reference ID</param>
        /// <returns>Transaction record</returns>
        Task<Transaction> CreateDepositAsync(string userId, string asset, decimal amount, string? reference = null);

        /// <summary>
        /// Creates a withdrawal request
        /// </summary>
        /// <param name="userId">The user ID (as string)</param>
        /// <param name="asset">Asset type (e.g., BTC, ETH)</param>
        /// <param name="amount">Withdrawal amount</param>
        /// <param name="address">Destination address</param>
        /// <param name="memo">Optional memo/tag for certain assets</param>
        /// <returns>Withdrawal request</returns>
        Task<Withdrawal> CreateWithdrawalAsync(
            string userId,
            string asset,
            decimal amount,
            string address,
            string? memo = null);

        /// <summary>
        /// Gets a withdrawal request by ID
        /// </summary>
        /// <param name="withdrawalId">The withdrawal ID as ObjectId</param>
        /// <param name="userId">The user ID as string</param>
        /// <returns>Withdrawal request or null if not found</returns>
        Task<Withdrawal?> GetWithdrawalAsync(ObjectId withdrawalId, string userId);

        /// <summary>
        /// Gets a withdrawal request by ID
        /// </summary>
        /// <param name="userId">The user ID (as string)</param>
        /// <param name="withdrawalId">The withdrawal ID (as string)</param>
        /// <returns>Withdrawal request or null if not found</returns>
        Task<Withdrawal?> GetWithdrawalByIdAsync(string userId, string withdrawalId);

        /// <summary>
        /// Gets available assets
        /// </summary>
        /// <returns>List of available asset symbols</returns>
        Task<List<string>> GetAvailableAssetsAsync();

        /// <summary>
        /// Ensures a user has an account, creating one if needed
        /// </summary>
        /// <param name="userId">The user ID (as string)</param>
        /// <returns>User account</returns>
        Task<Account> EnsureUserHasAccountAsync(string userId);

        /// <summary>
        /// Locks a balance amount for order execution
        /// </summary>
        /// <param name="request">Lock balance request</param>
        /// <returns>Lock balance response</returns>
        Task<LockBalanceResponse> LockBalanceAsync(LockBalanceRequest request);

        /// <summary>
        /// Unlocks a previously locked balance
        /// </summary>
        /// <param name="request">Unlock balance request</param>
        /// <returns>Unlock balance response</returns>
        Task<UnlockBalanceResponse> UnlockBalanceAsync(UnlockBalanceRequest request);

        /// <summary>
        /// Executes a trade between two users using locked balances
        /// </summary>
        /// <param name="request">Execute trade request</param>
        /// <returns>Execute trade response</returns>
        Task<ExecuteTradeResponse> ExecuteTradeAsync(ExecuteTradeRequest request);
    }
}