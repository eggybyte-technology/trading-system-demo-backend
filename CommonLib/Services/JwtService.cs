using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace CommonLib.Services
{
    /// <summary>
    /// Service for JWT token generation and validation
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="username">Username</param>
        /// <param name="email">Email</param>
        /// <param name="roles">User roles</param>
        /// <returns>JWT token and expiration time</returns>
        (string token, DateTime expiration) GenerateJwtToken(string userId, string username, string email, IEnumerable<string> roles);

        /// <summary>
        /// Generates a JWT token for a user with additional claims
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="username">Username</param>
        /// <param name="email">Email</param>
        /// <param name="roles">User roles</param>
        /// <param name="additionalClaims">Additional claims to include in the token</param>
        /// <returns>JWT token and expiration time</returns>
        (string token, DateTime expiration) GenerateJwtToken(string userId, string username, string email, IEnumerable<string> roles, IDictionary<string, string> additionalClaims);

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        /// <returns>Refresh token and expiration time</returns>
        (string token, DateTime expiration) GenerateRefreshToken();

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>ClaimsPrincipal if valid, null otherwise</returns>
        ClaimsPrincipal? ValidateToken(string token);
    }

    /// <summary>
    /// Implementation of JWT service
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the JWT service
        /// </summary>
        public JwtService(IConfiguration configuration, ILoggerService logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc/>
        public (string token, DateTime expiration) GenerateJwtToken(string userId, string username, string email, IEnumerable<string> roles)
        {
            return GenerateJwtToken(userId, username, email, roles, new Dictionary<string, string>());
        }

        /// <inheritdoc/>
        public (string token, DateTime expiration) GenerateJwtToken(string userId, string username, string email, IEnumerable<string> roles, IDictionary<string, string> additionalClaims)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWT configuration is missing or incomplete");
            }

            var expirationMinutes = _configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60);
            var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // Ensure we have all the important claims for user identification
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),                 // Primary subject identifier
                new Claim(ClaimTypes.NameIdentifier, userId),                   // Standard .NET identity claim
                new Claim("UserId", userId),                                    // Additional custom claim for API endpoints
                new Claim(JwtRegisteredClaimNames.Name, username),              // Username
                new Claim(JwtRegisteredClaimNames.Email, email),                // Email
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
            };

            // Add roles
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add additional claims
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }

            _logger.LogDebug($"Generating JWT with claims: {string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}"))}");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogDebug($"JWT token generated successfully: {tokenString.Substring(0, Math.Min(20, tokenString.Length))}...");

            return (tokenString, expiration);
        }

        /// <inheritdoc/>
        public (string token, DateTime expiration) GenerateRefreshToken()
        {
            var refreshTokenExpirationDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);
            var expiration = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
            var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            return (refreshToken, expiration);
        }

        /// <inheritdoc/>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            _logger.LogInformation($"Validating JWT Token: {token.Substring(0, Math.Min(20, token.Length))}...");

            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT validation failed: Configuration is missing or incomplete");
                throw new InvalidOperationException("JWT configuration is missing or incomplete");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            try
            {
                _logger.LogDebug($"JWT Validation Parameters: Issuer={issuer}, Audience={audience}, ClockSkew=Zero");

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Log claims for debugging
                _logger.LogInformation($"JWT Token validated successfully. Token expires: {validatedToken.ValidTo}");
                foreach (var claim in principal.Claims)
                {
                    _logger.LogDebug($"JWT Claim: {claim.Type}={claim.Value}");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return null;
            }
        }
    }
}