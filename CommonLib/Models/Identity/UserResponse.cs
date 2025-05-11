using System.Collections.Generic;

namespace CommonLib.Models.Identity
{
    /// <summary>
    /// User response model with non-sensitive user data
    /// </summary>
    public class UserResponse
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Phone number
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Whether email is verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Whether two-factor authentication is enabled
        /// </summary>
        public bool IsTwoFactorEnabled { get; set; }

        /// <summary>
        /// User roles
        /// </summary>
        public List<string> Roles { get; set; } = new();
    }
}