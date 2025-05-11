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
    /// Registration response model
    /// </summary>
    public class RegisterResponse
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
        /// JWT Access token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time (UNIX timestamp)
        /// </summary>
        public long Expiration { get; set; }
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
    /// Login response model
    /// </summary>
    public class LoginResponse
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
        /// JWT Access token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time (UNIX timestamp)
        /// </summary>
        public long Expiration { get; set; }
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
    /// Refresh token response model
    /// </summary>
    public class RefreshTokenResponse
    {
        /// <summary>
        /// JWT Access token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time (UNIX timestamp)
        /// </summary>
        public long Expiration { get; set; }
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