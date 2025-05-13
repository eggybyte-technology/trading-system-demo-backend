using CommonLib.Models.Identity;
using SimulationTest.Core;
using CommonLib.Api;
using System.Diagnostics;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Identity Service
    /// </summary>
    public class IdentityServiceTest
    {
        private readonly IdentityService _identityService;
        private readonly TestLogger _logger;
        private readonly StatusBar _statusBar;
        private readonly List<OperationResult> _results = new();
        private readonly TestContext _context;

        public IdentityServiceTest(
            IdentityService identityService,
            TestLogger logger,
            StatusBar statusBar,
            TestContext context)
        {
            _identityService = identityService;
            _logger = logger;
            _statusBar = statusBar;
            _context = context;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        public async Task<RegisterResponse> TestRegisterAsync()
        {
            string operationType = "IdentityService.RegisterAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                string username = $"unittest_user_{DateTime.Now:yyyyMMddHHmmss}";
                string email = $"{username}@example.com";
                string password = "Test@123";

                var registerRequest = new RegisterRequest
                {
                    Username = username,
                    Email = email,
                    Password = password
                };

                var result = await _identityService.RegisterAsync(registerRequest);

                // Store user info in context for later use
                _context.UserId = result.UserId;
                _context.Username = username;
                _context.Email = email;
                _context.Password = password;
                _context.Token = result.Token;
                _context.RefreshToken = result.RefreshToken;

                // Verify response
                if (string.IsNullOrEmpty(result.UserId))
                    throw new AssertionException("UserId should not be empty");
                if (string.IsNullOrEmpty(result.Token))
                    throw new AssertionException("Token should not be empty");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Login with registered user
        /// </summary>
        public async Task<LoginResponse> TestLoginAsync()
        {
            string operationType = "IdentityService.LoginAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var loginRequest = new LoginRequest
                {
                    Email = _context.Email,
                    Password = _context.Password
                };

                var result = await _identityService.LoginAsync(loginRequest);

                // Update token in context
                _context.Token = result.Token;
                _context.RefreshToken = result.RefreshToken;

                // Verify response
                if (string.IsNullOrEmpty(result.UserId))
                    throw new AssertionException("UserId should not be empty");
                if (result.UserId != _context.UserId)
                    throw new AssertionException($"UserId should match the registered user. Expected: {_context.UserId}, Got: {result.UserId}");
                if (string.IsNullOrEmpty(result.Token))
                    throw new AssertionException("Token should not be empty");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        public async Task<UserResponse> TestGetCurrentUserAsync()
        {
            string operationType = "IdentityService.GetCurrentUserAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _identityService.GetCurrentUserAsync(_context.Token);

                // Verify response
                if (result == null)
                    throw new AssertionException("User response should not be null");
                if (result.Username != _context.Username)
                    throw new AssertionException($"Username should match. Expected: {_context.Username}, Got: {result.Username}");
                if (result.Email != _context.Email)
                    throw new AssertionException($"Email should match. Expected: {_context.Email}, Got: {result.Email}");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update user information
        /// </summary>
        public async Task<UserResponse> TestUpdateUserAsync()
        {
            string operationType = "IdentityService.UpdateUserAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var updateRequest = new UpdateUserRequest
                {
                    Phone = "1234567890"
                };

                var result = await _identityService.UpdateUserAsync(_context.Token, updateRequest);

                // Verify response
                if (result == null)
                    throw new AssertionException("Updated user response should not be null");
                if (result.Phone != updateRequest.Phone)
                    throw new AssertionException($"Phone should be updated. Expected: {updateRequest.Phone}, Got: {result.Phone}");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        public async Task<RefreshTokenResponse> TestRefreshTokenAsync()
        {
            string operationType = "IdentityService.RefreshTokenAsync";
            _logger.Info($"Executing test: {operationType}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var refreshRequest = new RefreshTokenRequest
                {
                    RefreshToken = _context.RefreshToken
                };

                var result = await _identityService.RefreshTokenAsync(refreshRequest);

                // Update tokens in context
                _context.Token = result.Token;
                _context.RefreshToken = result.RefreshToken;

                // Verify response
                if (result == null)
                    throw new AssertionException("Refresh token response should not be null");
                if (string.IsNullOrEmpty(result.Token))
                    throw new AssertionException("New token should not be empty");

                stopwatch.Stop();
                ReportSuccess(operationType, stopwatch.ElapsedMilliseconds);
                _logger.Success($"Test passed: {operationType} ({stopwatch.ElapsedMilliseconds} ms)");

                return result;
            }
            catch (AssertionException ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - Assertion failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ReportFailure(operationType, ex.Message, stopwatch.ElapsedMilliseconds);
                _logger.Error($"Test failed: {operationType} - {ex.Message}");
                throw;
            }
        }

        private void ReportSuccess(string operationType, long latencyMs)
        {
            _results.Add(new OperationResult
            {
                OperationType = operationType,
                UserId = _context.UserId,
                Success = true,
                LatencyMs = latencyMs,
                Timestamp = DateTime.UtcNow
            });

            _statusBar.ReportSuccess(latencyMs);
        }

        private void ReportFailure(string operationType, string errorMessage, long latencyMs)
        {
            _results.Add(new OperationResult
            {
                OperationType = operationType,
                UserId = _context.UserId,
                Success = false,
                LatencyMs = latencyMs,
                Timestamp = DateTime.UtcNow,
                ErrorMessage = errorMessage
            });

            _statusBar.ReportFailure();
        }

        public List<OperationResult> GetResults() => _results;
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}