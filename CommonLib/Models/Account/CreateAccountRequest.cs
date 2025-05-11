using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Request to create a new account for a user
    /// </summary>
    public class CreateAccountRequest
    {
        /// <summary>
        /// User ID for whom the account should be created (from Identity service)
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Username for the account
        /// </summary>
        public string Username { get; set; }
    }

    /// <summary>
    /// Response from account creation
    /// </summary>
    public class CreateAccountResponse
    {
        /// <summary>
        /// The ID of the created account
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// The ID of the user who owns the account
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string Message { get; set; }
    }
}