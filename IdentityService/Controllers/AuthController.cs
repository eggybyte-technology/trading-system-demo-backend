using System;
using System.Threading.Tasks;
using System.Text.Json;
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
using CommonLib.Api;

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
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private readonly CommonLib.Api.AccountService _accountService;

        /// <summary>
        /// Initializes a new instance of the AuthController
        /// </summary>
        public AuthController(
            IUserRepository userRepository,
            ISecurityTokenRepository tokenRepository,
            JwtService jwtService,
            ILoggerService logger,
            IApiLoggingService apiLogger,
            IHttpClientService httpClientService,
            CommonLib.Api.AccountService accountService)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _jwtService = jwtService;
            _logger = logger;
            _apiLogger = apiLogger;
            _httpClientService = httpClientService;
            _accountService = accountService;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>New user details and authentication tokens</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    var errorResponse = new { message = "Username is required", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    var errorResponse = new { message = "Email is required", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                {
                    var errorResponse = new { message = "Password must be at least 8 characters", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    var errorResponse = new { message = "Invalid email format", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Check if username already exists
                if (await _userRepository.IsUsernameTakenAsync(request.Username))
                {
                    var errorResponse = new { message = "Username already taken", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Check if email already exists
                if (await _userRepository.IsEmailRegisteredAsync(request.Email))
                {
                    var errorResponse = new { message = "Email already registered", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

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

                    // Call Account service to create an account using the API client
                    var accountResponse = await _accountService.CreateAccountAsync(serviceToken, accountRequest);

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
                var registerResponse = new RegisterResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = new DateTimeOffset(expiration).ToUnixTimeSeconds()
                };

                var response = new { data = registerResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration: {ex.Message}");
                var errorResponse = new { message = "An error occurred during registration", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>User details and authentication tokens</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    var errorResponse = new { message = "Email is required", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    var errorResponse = new { message = "Password is required", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                // Find user by email
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    var errorResponse = new { message = "Invalid email or password", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Verify password
                if (!BC.Verify(request.Password, user.HashedPassword))
                {
                    var errorResponse = new { message = "Invalid email or password", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    var errorResponse = new { message = "Account is disabled", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Generate tokens
                var (token, refreshToken, expiration) = await GenerateTokensForUser(user);

                // Return user info and tokens
                var loginResponse = new LoginResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = new DateTimeOffset(expiration).ToUnixTimeSeconds()
                };

                var response = new { data = loginResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login: {ex.Message}");
                var errorResponse = new { message = "An error occurred during login", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Refreshes an authentication token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>New authentication tokens</returns>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(RefreshTokenResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate the request
                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    var errorResponse = new { message = "Refresh token is required", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return BadRequest(errorResponse);
                }

                _logger.LogDebug($"Processing refresh token request: {request.RefreshToken.Substring(0, Math.Min(10, request.RefreshToken.Length))}...");

                // Find token in database
                var storedToken = await _tokenRepository.GetByTokenValueAsync(request.RefreshToken);
                if (storedToken == null)
                {
                    var errorResponse = new { message = "Invalid refresh token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Check if token is valid
                if (storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    var errorResponse = new { message = "Refresh token has expired", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                if (storedToken.IsRevoked)
                {
                    var errorResponse = new { message = "Refresh token has been revoked", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Get user
                var user = await _userRepository.GetByIdAsync(storedToken.UserId);
                if (user == null)
                {
                    var errorResponse = new { message = "User not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                _logger.LogDebug($"User found for refresh token: {user.Username} ({user.Id})");

                // Revoke the old token
                await _tokenRepository.RevokeAsync(storedToken.Id);
                _logger.LogDebug($"Old refresh token revoked: {storedToken.Id}");

                // Generate new tokens
                var (token, refreshToken, expiration) = await GenerateTokensForUser(user);
                _logger.LogDebug($"New tokens generated: JWT token ({token.Substring(0, Math.Min(10, token.Length))}...), Refresh token ({refreshToken.Substring(0, Math.Min(10, refreshToken.Length))}...)");

                // Return new tokens
                var refreshTokenResponse = new RefreshTokenResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = new DateTimeOffset(expiration).ToUnixTimeSeconds()
                };

                var response = new { data = refreshTokenResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing token: {ex.Message}");
                var errorResponse = new { message = "An error occurred while refreshing the token", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Gets the current user's information
        /// </summary>
        /// <returns>User details</returns>
        [HttpGet("user")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCurrentUser()
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

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
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                if (!ObjectId.TryParse(userIdClaim, out var userId))
                {
                    var errorResponse = new { message = "Invalid authentication token format", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                _logger.LogDebug($"Found user ID in claims: {userId}");

                // Get user
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    var errorResponse = new { message = "User not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                _logger.LogDebug($"User retrieved from database: {user.Username} ({user.Id})");

                // Return user info (excluding sensitive data)
                var userResponse = new UserResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsEmailVerified = user.IsEmailVerified,
                    IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                    Roles = user.Roles.Select(r => r.Name).ToList()
                };

                var response = new { data = userResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user: {ex.Message}");
                var errorResponse = new { message = "An error occurred while retrieving user information", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Updates the current user's information
        /// </summary>
        /// <param name="request">Updated user information</param>
        /// <returns>Updated user details</returns>
        [HttpPut("user")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            await _apiLogger.LogApiRequest(HttpContext);
            var startTime = DateTime.UtcNow;

            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ObjectId.TryParse(userIdClaim, out var userId))
                {
                    var errorResponse = new { message = "Invalid authentication token", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return Unauthorized(errorResponse);
                }

                // Get user
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    var errorResponse = new { message = "User not found", success = false };
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                    return NotFound(errorResponse);
                }

                // Check password if provided or if changing email
                if (!string.IsNullOrEmpty(request.CurrentPassword) || !string.IsNullOrEmpty(request.NewPassword) || !string.IsNullOrEmpty(request.Email))
                {
                    if (string.IsNullOrEmpty(request.CurrentPassword))
                    {
                        var errorResponse = new { message = "Current password is required", success = false };
                        var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                        await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                        return BadRequest(errorResponse);
                    }

                    if (!BC.Verify(request.CurrentPassword, user.HashedPassword))
                    {
                        var errorResponse = new { message = "Current password is incorrect", success = false };
                        var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                        await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                        return BadRequest(errorResponse);
                    }
                }

                // Update email
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    // Validate email format
                    if (!IsValidEmail(request.Email))
                    {
                        var errorResponse = new { message = "Invalid email format", success = false };
                        var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                        await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                        return BadRequest(errorResponse);
                    }

                    // Check if email is already registered
                    if (await _userRepository.IsEmailRegisteredAsync(request.Email))
                    {
                        var errorResponse = new { message = "Email already registered", success = false };
                        var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                        await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                        return BadRequest(errorResponse);
                    }

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
                    {
                        var errorResponse = new { message = "Password must be at least 8 characters", success = false };
                        var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                        await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                        return BadRequest(errorResponse);
                    }

                    user.HashedPassword = BC.HashPassword(request.NewPassword);
                }

                // Update timestamp
                user.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _userRepository.UpdateAsync(user);

                // Return updated user info
                var userResponse = new UserResponse
                {
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsEmailVerified = user.IsEmailVerified,
                    IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                    Roles = user.Roles.Select(r => r.Name).ToList()
                };

                var response = new { data = userResponse, success = true };
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, responseJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user: {ex.Message}");
                var errorResponse = new { message = "An error occurred while updating user information", success = false };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await _apiLogger.LogApiResponse(HttpContext, errorJson, (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
                return StatusCode(500, errorResponse);
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