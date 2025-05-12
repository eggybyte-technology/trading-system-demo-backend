using System.Collections.Generic;

namespace CommonLib.Models.Identity
{
    /// <summary>
    /// Registration request model
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Phone number (optional)
        /// </summary>
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Refresh token request model
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update user request model
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Email (optional)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Phone number (optional)
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Current password (required for verification)
        /// </summary>
        public string? CurrentPassword { get; set; }

        /// <summary>
        /// New password (optional)
        /// </summary>
        public string? NewPassword { get; set; }
    }
}