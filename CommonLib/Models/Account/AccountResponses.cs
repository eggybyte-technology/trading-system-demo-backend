using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Response model for account balance information
    /// </summary>
    public class BalanceResponse
    {
        /// <summary>
        /// List of balance records
        /// </summary>
        public List<BalanceInfo> Balances { get; set; } = new List<BalanceInfo>();
    }

    /// <summary>
    /// Balance information
    /// </summary>
    public class BalanceInfo
    {
        /// <summary>
        /// Asset code
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Free (available) amount
        /// </summary>
        public decimal Free { get; set; }

        /// <summary>
        /// Locked (unavailable) amount
        /// </summary>
        public decimal Locked { get; set; }

        /// <summary>
        /// Total amount (Free + Locked)
        /// </summary>
        public decimal Total => Free + Locked;

        /// <summary>
        /// Last update timestamp in milliseconds
        /// </summary>
        public long UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response model for transaction list
    /// </summary>
    public class TransactionListResponse
    {
        /// <summary>
        /// Total number of transactions
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Current page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// List of transaction items
        /// </summary>
        public List<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }

    /// <summary>
    /// Transaction item for API response
    /// </summary>
    public class TransactionItem
    {
        /// <summary>
        /// Transaction ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Asset code
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Transaction type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Transaction status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Transaction timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Optional reference information
        /// </summary>
        public string? Reference { get; set; }
    }

    /// <summary>
    /// Response model for deposit operation
    /// </summary>
    public class DepositResponse
    {
        /// <summary>
        /// Transaction ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Asset code
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Deposit amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Transaction type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Status of the transaction
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Transaction timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Optional reference information
        /// </summary>
        public string? Reference { get; set; }
    }

    /// <summary>
    /// Response model for withdrawal request
    /// </summary>
    public class WithdrawalResponse
    {
        /// <summary>
        /// Withdrawal request ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Asset code
        /// </summary>
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Withdrawal amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Destination address
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Status of the withdrawal
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Transaction timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Blockchain transaction ID, if available
        /// </summary>
        public string? TransactionId { get; set; }
    }

    /// <summary>
    /// Response model for creating a new account
    /// </summary>
    public class CreateAccountResponse
    {
        /// <summary>
        /// Account ID
        /// </summary>
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Creation timestamp in milliseconds
        /// </summary>
        public long CreatedAt { get; set; }

        /// <summary>
        /// Message describing the result of the account creation
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for available assets
    /// </summary>
    public class AssetListResponse
    {
        /// <summary>
        /// List of available assets
        /// </summary>
        public List<AssetInfo> Assets { get; set; } = new List<AssetInfo>();
    }

    /// <summary>
    /// Asset information
    /// </summary>
    public class AssetInfo
    {
        /// <summary>
        /// Asset symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Asset name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the asset is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether deposits are enabled
        /// </summary>
        public bool CanDeposit { get; set; } = true;

        /// <summary>
        /// Whether withdrawals are enabled
        /// </summary>
        public bool CanWithdraw { get; set; } = true;

        /// <summary>
        /// Minimum deposit amount
        /// </summary>
        public decimal MinDepositAmount { get; set; }

        /// <summary>
        /// Minimum withdrawal amount
        /// </summary>
        public decimal MinWithdrawalAmount { get; set; }

        /// <summary>
        /// Fixed withdrawal fee
        /// </summary>
        public decimal WithdrawalFeeFixed { get; set; }

        /// <summary>
        /// Percentage withdrawal fee
        /// </summary>
        public decimal WithdrawalFeePercentage { get; set; }
    }

    /// <summary>
    /// Response from a balance lock operation
    /// </summary>
    public class LockBalanceResponse
    {
        /// <summary>
        /// Indicates if the lock was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique lock identifier
        /// </summary>
        public string LockId { get; set; }

        /// <summary>
        /// Timestamp when the lock expires (in milliseconds)
        /// </summary>
        public long ExpirationTimestamp { get; set; }

        /// <summary>
        /// Error message if the lock failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Response from a balance unlock operation
    /// </summary>
    public class UnlockBalanceResponse
    {
        /// <summary>
        /// Indicates if the unlock was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the unlock failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Response from a trade execution
    /// </summary>
    public class ExecuteTradeResponse
    {
        /// <summary>
        /// Indicates if the trade was successfully executed
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique identifier for the created trade
        /// </summary>
        public string TradeId { get; set; }

        /// <summary>
        /// Error message if the trade execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}