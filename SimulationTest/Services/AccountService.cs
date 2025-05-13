using CommonLib.Models.Account;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Implementation of the account service for balance, transactions, etc.
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly IHttpClientService _httpClient;

        /// <summary>
        /// Initializes a new instance of the AccountService
        /// </summary>
        /// <param name="httpClient">HTTP client service</param>
        public AccountService(IHttpClientService httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets account balance for the authenticated user
        /// </summary>
        /// <returns>Account balance response</returns>
        public async Task<BalanceResponse> GetBalanceAsync()
        {
            return await _httpClient.GetAsync<BalanceResponse>("account", "account/balance");
        }

        /// <summary>
        /// Gets transaction history for the authenticated user
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="startTime">Start time in Unix timestamp</param>
        /// <param name="endTime">End time in Unix timestamp</param>
        /// <param name="type">Transaction type filter</param>
        /// <returns>Transaction history response</returns>
        public async Task<TransactionListResponse> GetTransactionsAsync(
            int page = 1,
            int pageSize = 20,
            long? startTime = null,
            long? endTime = null,
            string type = null)
        {
            var queryParams = $"page={page}&pageSize={pageSize}";

            if (startTime.HasValue)
                queryParams += $"&startTime={startTime.Value}";

            if (endTime.HasValue)
                queryParams += $"&endTime={endTime.Value}";

            if (!string.IsNullOrEmpty(type))
                queryParams += $"&type={type}";

            return await _httpClient.GetAsync<TransactionListResponse>("account", "account/transactions", queryParams);
        }

        /// <summary>
        /// Creates a deposit for the authenticated user
        /// </summary>
        /// <param name="request">Deposit request</param>
        /// <returns>Deposit response</returns>
        public async Task<DepositResponse> CreateDepositAsync(DepositRequest request)
        {
            return await _httpClient.PostAsync<DepositRequest, DepositResponse>("account", "account/deposit", request);
        }

        /// <summary>
        /// Creates a withdrawal request for the authenticated user
        /// </summary>
        /// <param name="request">Withdrawal request</param>
        /// <returns>Withdrawal response</returns>
        public async Task<WithdrawalResponse> CreateWithdrawalAsync(WithdrawalRequest request)
        {
            return await _httpClient.PostAsync<WithdrawalRequest, WithdrawalResponse>("account", "account/withdraw", request);
        }

        /// <summary>
        /// Gets withdrawal request status by ID
        /// </summary>
        /// <param name="id">Withdrawal request ID</param>
        /// <returns>Withdrawal information</returns>
        public async Task<Withdrawal> GetWithdrawalAsync(string id)
        {
            return await _httpClient.GetAsync<Withdrawal>("account", $"account/withdrawals/{id}");
        }

        /// <summary>
        /// Gets available assets
        /// </summary>
        /// <returns>Asset list response</returns>
        public async Task<AssetListResponse> GetAvailableAssetsAsync()
        {
            return await _httpClient.GetAsync<AssetListResponse>("account", "account/assets");
        }
    }
}