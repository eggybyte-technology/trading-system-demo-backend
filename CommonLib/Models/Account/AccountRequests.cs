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
}