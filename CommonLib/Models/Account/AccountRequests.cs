using System;
using MongoDB.Bson;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Request model for creating a deposit
    /// </summary>
    public class DepositRequest
    {
        /// <summary>
        /// Asset to deposit
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Amount to deposit
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Optional reference information
        /// </summary>
        public string? Reference { get; set; }
    }

    /// <summary>
    /// Request model for creating a withdrawal
    /// </summary>
    public class WithdrawalRequest
    {
        /// <summary>
        /// Asset to withdraw
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Amount to withdraw
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Destination address
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Optional memo
        /// </summary>
        public string? Memo { get; set; }
    }

    /// <summary>
    /// Request model for creating a new account
    /// </summary>
    public class CreateAccountRequest
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for retrieving transaction history
    /// </summary>
    public class TransactionListRequest
    {
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Start time in Unix timestamp (milliseconds)
        /// </summary>
        public long? StartTime { get; set; }

        /// <summary>
        /// End time in Unix timestamp (milliseconds)
        /// </summary>
        public long? EndTime { get; set; }

        /// <summary>
        /// Transaction type filter
        /// </summary>
        public string? Type { get; set; }
    }

    /// <summary>
    /// Request to lock a user's balance for a trade
    /// </summary>
    public class LockBalanceRequest
    {
        /// <summary>
        /// User ID whose balance is being locked
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Asset symbol to lock
        /// </summary>
        public string Asset { get; set; }

        /// <summary>
        /// Amount to lock
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Order ID associated with this lock
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Unique lock identifier (optional, will be generated if not provided)
        /// </summary>
        public string LockId { get; set; } = string.Empty;

        /// <summary>
        /// Timeout for lock in seconds (default: 5 seconds)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 5;
    }

    /// <summary>
    /// Request to unlock a previously locked balance
    /// </summary>
    public class UnlockBalanceRequest
    {
        /// <summary>
        /// User ID whose balance is being unlocked
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Asset symbol to unlock
        /// </summary>
        public string Asset { get; set; }

        /// <summary>
        /// Unique lock identifier to release
        /// </summary>
        public string LockId { get; set; }
    }

    /// <summary>
    /// Request to execute a trade using locked balances
    /// </summary>
    public class ExecuteTradeRequest
    {
        /// <summary>
        /// Buy order identifier
        /// </summary>
        public string BuyOrderId { get; set; }

        /// <summary>
        /// Sell order identifier
        /// </summary>
        public string SellOrderId { get; set; }

        /// <summary>
        /// User ID of the buyer
        /// </summary>
        public string BuyerUserId { get; set; }

        /// <summary>
        /// User ID of the seller
        /// </summary>
        public string SellerUserId { get; set; }

        /// <summary>
        /// Base asset of the trading pair
        /// </summary>
        public string BaseAsset { get; set; }

        /// <summary>
        /// Quote asset of the trading pair
        /// </summary>
        public string QuoteAsset { get; set; }

        /// <summary>
        /// Trade quantity in base asset
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Trade price in quote asset
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Lock ID for the buyer's funds
        /// </summary>
        public string BuyLockId { get; set; }

        /// <summary>
        /// Lock ID for the seller's funds
        /// </summary>
        public string SellLockId { get; set; }

        /// <summary>
        /// Unique identifier for this match
        /// </summary>
        public string MatchId { get; set; }
    }
}