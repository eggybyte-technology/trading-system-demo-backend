using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CommonLib.Models.Identity;
using CommonLib.Models.Account;
using IdentityService.Repositories;
using CommonLib.Services;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using System.Net;
using BC = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;

namespace IdentityService.Controllers
{
    /// <summary>
    /// Controller handling authentication and user management
    /// </summary>
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ISecurityTokenRepository _tokenRepository;
        private readonly JwtService _jwtService;
        private readonly ILoggerService _logger;
        private readonly IApiLoggingService _apiLogger;
        private readonly IHttpClientService _httpClientService;

        /// <summary>
        /// Initializes a new instance of the AuthController
        /// </summary>
        public AuthController(
            IUserRepository userRepository,
            ISecurityTokenRepository tokenRepository,
            JwtService jwtService,
            ILoggerService logger,
            IApiLoggingService apiLogger,
            IHttpClientService httpClientService)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _jwtService = jwtService;
            _logger = logger;
            _apiLogger = apiLogger;
            _httpClientService = httpClientService;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>New user details and authentication tokens</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest(new { message = "Username is required" });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { message = "Email is required" });

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return BadRequest(new { message = "Password must be at least 8 characters" });

                // Validate email format
                if (!IsValidEmail(request.Email))
                    return BadRequest(new { message = "Invalid email format" });

                // Check if username already exists
                if (await _userRepository.IsUsernameTakenAsync(request.Username))
                    return BadRequest(new { message = "Username already taken" });

                // Check if email already exists
                if (await _userRepository.IsEmailRegisteredAsync(request.Email))
                    return BadRequest(new { message = "Email already registered" });

                // Create user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    HashedPassword = BC.HashPassword(request.Password),
                    Phone = request.Phone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Roles = new() { new Role { Name = "User", Permissions = new() { "basic" } } }
                };

                await _userRepository.CreateAsync(user);
                _logger.LogInformation($"User created: {user.Id}, {user.Username}");

                // Generate tokens for user
                var (token, refreshToken, expiration) = await GenerateTokensForUser(user);

                // Create account in AccountService
                try
                {
                    var accountRequest = new CreateAccountRequest
                    {
                        UserId = user.Id.ToString(),
                        Username = user.Username
                    };

                    _logger.LogInformation($"Creating account for user: {user.Id}");

                    // Add a service claim to the token to identify this call is coming from the IdentityService
                    var serviceSpecificClaims = new Dictionary<string, string>
                    {
                        { "ServiceName", "IdentityService" },
                        { "ServiceAction", "AccountCreation" }
                    };

                    // Generate a special service token with service claims
                    var (serviceToken, _) = _jwtService.GenerateJwtToken(
                        userId: user.Id.ToString(),
                        username: "identity-service",
                        email: "service@identity.trading-system.local",
                        roles: new List<string> { "Service" },
                        additionalClaims: serviceSpecificClaims);

                    // Call Account service to create an account
                    var accountResponse = await _httpClientService.PostAsync<CreateAccountRequest, CreateAccountResponse>(
                        "AccountService",
                        "account/create",
                        accountRequest,
                        serviceToken);

                    if (accountResponse != null)
                    {
                        _logger.LogInformation($"Account created successfully for user {user.Id}. Account ID: {accountResponse.AccountId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Account creation response was null for user {user.Id}");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail registration
                    _logger.LogError($"Error creating account for user {user.Id}: {ex.Message}");
                    _logger.LogError($"Exception details: {ex}");
                    // We don't want to fail the registration if account creation fails
                    // The account can be created later
                }

                // Return user info and tokens
                return Ok(new RegisterResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = new DateTimeOffset(expiration).ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>User details and authentication tokens</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { message = "Email is required" });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new { message = "Password is required" });

                // Find user by email
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                // Verify password
                if (!BC.Verify(request.Password, user.HashedPassword))
                    return Unauthorized(new { message = "Invalid email or password" });

                // Check if account is active
                if (!user.IsActive)
                    return Unauthorized(new { message = "Account is disabled" });

                // Generate tokens
                var (token, refreshToken, expiration) = await GenerateTokensForUser(user);

                // Return user info and tokens
                return Ok(new LoginResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = new DateTimeOffset(expiration).ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Refreshes an authentication token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>New authentication tokens</returns>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Validate the request
                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    _logger.LogWarning("Refresh token is required but was not provided");
                    return BadRequest(new { message = "Refresh token is required" });
                }

                _logger.LogDebug($"Processing refresh token request: {request.RefreshToken.Substring(0, Math.Min(10, request.RefreshToken.Length))}...");

                // Find token in database
                var storedToken = await _tokenRepository.GetByTokenValueAsync(request.RefreshToken);
                if (storedToken == null)
                {
                    _logger.LogWarning($"Refresh token not found in database: {request.RefreshToken.Substring(0, Math.Min(10, request.RefreshToken.Length))}...");
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                // Check if token is valid
                if (storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Refresh token expired: {storedToken.ExpiresAt}");
                    return Unauthorized(new { message = "Refresh token has expired" });
                }

                if (storedToken.IsRevoked)
                {
                    _logger.LogWarning($"Refresh token has been revoked: {storedToken.Id}");
                    return Unauthorized(new { message = "Refresh token has been revoked" });
                }

                // Get user
                var user = await _userRepository.GetByIdAsync(storedToken.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for refresh token. User ID: {storedToken.UserId}");
                    return Unauthorized(new { message = "User not found" });
                }

                _logger.LogDebug($"User found for refresh token: {user.Username} ({user.Id})");

                // Revoke the old token
                await _tokenRepository.RevokeAsync(storedToken.Id);
                _logger.LogDebug($"Old refresh token revoked: {storedToken.Id}");

                // Generate new tokens
                var (token, refreshToken, expiration) = await GenerateTokensForUser(user);
                _logger.LogDebug($"New tokens generated: JWT token ({token.Substring(0, Math.Min(10, token.Length))}...), Refresh token ({refreshToken.Substring(0, Math.Min(10, refreshToken.Length))}...)");

                // Return new tokens
                return Ok(new RefreshTokenResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = new DateTimeOffset(expiration).ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing token: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while refreshing the token" });
            }
        }

        /// <summary>
        /// Gets the current user's information
        /// </summary>
        /// <returns>User details</returns>
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Debug authentication state
                _logger.LogDebug($"Authentication state in GetCurrentUser: IsAuthenticated={User.Identity?.IsAuthenticated}, Name={User.Identity?.Name}");
                _logger.LogDebug($"Claims in GetCurrentUser:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogDebug($"  {claim.Type}={claim.Value}");
                }

                // Get user ID from claims - Most important is 'sub' (subject) claim for userId
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                  User.FindFirst("sub")?.Value ??
                                  User.FindFirst("userId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("Invalid authentication token: Cannot find any user ID claim");
                    return Unauthorized(new { message = "Invalid authentication token" });
                }

                if (!ObjectId.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning($"Invalid user ID format: {userIdClaim}");
                    return Unauthorized(new { message = "Invalid authentication token format" });
                }

                _logger.LogDebug($"Found user ID in claims: {userId}");

                // Get user
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for ID: {userId}");
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogDebug($"User retrieved from database: {user.Username} ({user.Id})");

                // Return user info (excluding sensitive data)
                return Ok(new UserResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsEmailVerified = user.IsEmailVerified,
                    IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                    Roles = user.Roles.Select(r => r.Name).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving user information" });
            }
        }

        /// <summary>
        /// Updates the current user's information
        /// </summary>
        /// <param name="request">Updated user information</param>
        /// <returns>Updated user details</returns>
        [HttpPut("user")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                // Get user
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Check password if provided or if changing email
                if (!string.IsNullOrEmpty(request.CurrentPassword) || !string.IsNullOrEmpty(request.NewPassword) || !string.IsNullOrEmpty(request.Email))
                {
                    if (string.IsNullOrEmpty(request.CurrentPassword))
                        return BadRequest(new { message = "Current password is required" });

                    if (!BC.Verify(request.CurrentPassword, user.HashedPassword))
                        return BadRequest(new { message = "Current password is incorrect" });
                }

                // Update email
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    // Validate email format
                    if (!IsValidEmail(request.Email))
                        return BadRequest(new { message = "Invalid email format" });

                    // Check if email is already registered
                    if (await _userRepository.IsEmailRegisteredAsync(request.Email))
                        return BadRequest(new { message = "Email already registered" });

                    user.Email = request.Email;
                    user.IsEmailVerified = false; // Require verification for new email
                }

                // Update phone
                if (request.Phone != null)
                {
                    user.Phone = request.Phone;
                }

                // Update password
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    if (request.NewPassword.Length < 8)
                        return BadRequest(new { message = "Password must be at least 8 characters" });

                    user.HashedPassword = BC.HashPassword(request.NewPassword);
                }

                // Update timestamp
                user.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _userRepository.UpdateAsync(user);

                // Return updated user info
                return Ok(new UserResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsEmailVerified = user.IsEmailVerified,
                    IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                    Roles = user.Roles.Select(r => r.Name).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating user information" });
            }
        }

        /// <summary>
        /// Generates JWT and refresh tokens for a user
        /// </summary>
        /// <param name="user">The user</param>
        /// <returns>JWT token, refresh token, and expiration</returns>
        private async Task<(string Token, string RefreshToken, DateTime Expiration)> GenerateTokensForUser(User user)
        {
            // Generate JWT token
            var (token, expiration) = _jwtService.GenerateJwtToken(
                userId: user.Id.ToString(),
                username: user.Username,
                email: user.Email,
                roles: user.Roles.Select(r => r.Name).ToList());

            // Generate refresh token
            var refreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            // Save refresh token to database
            await _tokenRepository.CreateAsync(new SecurityToken
            {
                UserId = user.Id,
                Token = refreshToken,
                Type = "refresh",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false
            });

            return (token, refreshToken, expiration);
        }

        /// <summary>
        /// Validates an email address format
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if the email is valid</returns>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Simple regex for basic email validation
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}