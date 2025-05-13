using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommonLib.Models;
using CommonLib.Models.Account;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    public class AccountService : BaseService
    {
        public AccountService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "AccountService", "http://account.trading-system.local", logger)
        {
        }

        public async Task<BalanceResponse> GetBalanceAsync(string token)
        {
            return await GetAsync<BalanceResponse>("/account/balance", token);
        }

        public async Task<TransactionListResponse> GetTransactionsAsync(string token, int page = 1, int pageSize = 20, long? startTime = null, long? endTime = null, string? type = null)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["startTime"] = startTime?.ToString(),
                ["endTime"] = endTime?.ToString(),
                ["type"] = type
            };

            var queryString = BuildQueryString(queryParams);
            return await GetAsync<TransactionListResponse>($"/account/transactions?{queryString}", token);
        }

        public async Task<DepositResponse> CreateDepositAsync(string token, DepositRequest request)
        {
            return await PostAsync<DepositResponse, DepositRequest>("/account/deposit", request, token);
        }

        public async Task<WithdrawalResponse> CreateWithdrawalAsync(string token, WithdrawalRequest request)
        {
            return await PostAsync<WithdrawalResponse, WithdrawalRequest>("/account/withdraw", request, token);
        }

        public async Task<WithdrawalResponse> GetWithdrawalStatusAsync(string token, string withdrawalId)
        {
            return await GetAsync<WithdrawalResponse>($"/account/withdrawals/{withdrawalId}", token);
        }

        public async Task<AssetListResponse> GetAssetsAsync(string token)
        {
            return await GetAsync<AssetListResponse>("/account/assets", token);
        }

        public async Task<CreateAccountResponse> CreateAccountAsync(string token, CreateAccountRequest request)
        {
            return await PostAsync<CreateAccountResponse, CreateAccountRequest>("/account/create", request, token);
        }

        /// <summary>
        /// Lock a user's balance for a trading operation
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Balance lock request</param>
        /// <returns>Lock balance response with status</returns>
        public async Task<LockBalanceResponse> LockBalanceAsync(string token, LockBalanceRequest request)
        {
            return await PostAsync<LockBalanceResponse, LockBalanceRequest>("/account/lock-balance", request, token);
        }

        /// <summary>
        /// Unlock a previously locked balance
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Balance unlock request</param>
        /// <returns>Unlock balance response with status</returns>
        public async Task<UnlockBalanceResponse> UnlockBalanceAsync(string token, UnlockBalanceRequest request)
        {
            return await PostAsync<UnlockBalanceResponse, UnlockBalanceRequest>("/account/unlock-balance", request, token);
        }

        /// <summary>
        /// Execute a trade between two users using locked balances
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <param name="request">Trade execution request</param>
        /// <returns>Trade execution response with status</returns>
        public async Task<ExecuteTradeResponse> ExecuteTradeAsync(string token, ExecuteTradeRequest request)
        {
            return await PostAsync<ExecuteTradeResponse, ExecuteTradeRequest>("/account/execute-trade", request, token);
        }
    }
}