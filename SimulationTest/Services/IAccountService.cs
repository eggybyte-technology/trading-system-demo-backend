using CommonLib.Models.Account;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for account operations (balance, transactions, etc.)
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Gets account balance for the authenticated user
        /// </summary>
        /// <returns>Account balance response</returns>
        Task<BalanceResponse> GetBalanceAsync();

        /// <summary>
        /// Gets transaction history for the authenticated user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="startTime">Start time in Unix timestamp (optional)</param>
        /// <param name="endTime">End time in Unix timestamp (optional)</param>
        /// <param name="type">Transaction type filter (optional)</param>
        /// <returns>Transaction history response</returns>
        Task<TransactionListResponse> GetTransactionsAsync(
            int page = 1,
            int pageSize = 20,
            long? startTime = null,
            long? endTime = null,
            string type = null);

        /// <summary>
        /// Creates a deposit for the authenticated user
        /// </summary>
        /// <param name="request">Deposit request</param>
        /// <returns>Deposit response</returns>
        Task<DepositResponse> CreateDepositAsync(DepositRequest request);

        /// <summary>
        /// Creates a withdrawal request for the authenticated user
        /// </summary>
        /// <param name="request">Withdrawal request</param>
        /// <returns>Withdrawal response</returns>
        Task<WithdrawalResponse> CreateWithdrawalAsync(WithdrawalRequest request);

        /// <summary>
        /// Gets withdrawal request status by ID
        /// </summary>
        /// <param name="id">Withdrawal request ID</param>
        /// <returns>Withdrawal information</returns>
        Task<Withdrawal> GetWithdrawalAsync(string id);

        /// <summary>
        /// Gets available assets
        /// </summary>
        /// <returns>Asset list response</returns>
        Task<AssetListResponse> GetAvailableAssetsAsync();
    }
}