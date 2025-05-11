using System;

namespace CommonLib.Models.Identity
{
    /// <summary>
    /// Represents user authentication credentials for simulation purposes
    /// </summary>
    public class UserCredential
    {
        /// <summary>
        /// The user's unique identifier
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The user's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The user's authentication token
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}