using System;
using System.Threading.Tasks;
using CommonLib.Models.Identity;
using SimulationTest.Core;
using System.Diagnostics;
using MongoDB.Bson;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for the Identity Service API
    /// </summary>
    public class IdentityServiceTests : ApiTestBase
    {
        private static string _testUsername;
        private static string _testPassword;
        private static string _testEmail;
        private static SecurityToken _securityToken;
        private static readonly string TestDependencyPrefix = "SimulationTest.Tests.";

        /// <summary>
        /// Test connectivity to Identity Service before running tests
        /// </summary>
        [ApiTest("Test connectivity to Identity Service")]
        public async Task<ApiTestResult> CheckConnectivity_IdentityService_ShouldBeAccessible()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Try to connect to the health endpoint
                var client = _httpClientFactory.GetClient("identity");
                var response = await client.GetAsync("/health");

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    // Generate test credentials for subsequent tests
                    _testUsername = $"testuser_{Guid.NewGuid():N}";
                    _testPassword = "Password123!";
                    _testEmail = $"{_testUsername}@example.com";

                    return ApiTestResult.Passed(stopwatch.Elapsed);
                }
                else
                {
                    return ApiTestResult.Failed(
                        $"Failed to connect to Identity Service. Status code: {response.StatusCode}",
                        null,
                        stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception while connecting to Identity Service: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that a user can register successfully
        /// </summary>
        [ApiTest("Test registration with valid data", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.CheckConnectivity_IdentityService_ShouldBeAccessible" })]
        public async Task<ApiTestResult> Register_WithValidData_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                if (_testUsername == null)
                {
                    _testUsername = $"testuser_{Guid.NewGuid():N}";
                    _testPassword = "Password123!";
                    _testEmail = $"{_testUsername}@example.com";
                }

                var registerRequest = new RegisterRequest
                {
                    Username = _testUsername,
                    Password = _testPassword,
                    Email = _testEmail
                };

                // Act
                var response = await PostAsync<RegisterRequest, UserResponse>("identity", "/auth/register", registerRequest, false);

                // Assert
                stopwatch.Stop();

                if (response == null)
                {
                    return ApiTestResult.Failed(
                        "Registration response is null",
                        null,
                        stopwatch.Elapsed);
                }

                bool isValid = response.Username == _testUsername &&
                               response.Email == _testEmail;

                if (!isValid)
                {
                    return ApiTestResult.Failed(
                        $"User data mismatch. Expected Username={_testUsername}, Email={_testEmail}, " +
                        $"but received Username={response.Username}, Email={response.Email}",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that a user can login successfully
        /// </summary>
        [ApiTest("Test login with valid credentials", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Register_WithValidData_ShouldSucceed" })]
        public async Task<ApiTestResult> Login_WithValidCredentials_ShouldReturnToken()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Use the credentials from registration test
                if (_testUsername == null)
                {
                    return ApiTestResult.Failed(
                        "Registration test should have run first to create test credentials",
                        null,
                        stopwatch.Elapsed);
                }

                // Act
                var loginRequest = new LoginRequest
                {
                    Email = _testEmail,
                    Password = _testPassword
                };

                var loginResponse = await PostAsync<LoginRequest, SecurityToken>("identity", "/auth/login", loginRequest, false);

                // Assert
                stopwatch.Stop();

                if (loginResponse == null)
                {
                    return ApiTestResult.Failed(
                        "Login response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (string.IsNullOrWhiteSpace(loginResponse.Token))
                {
                    return ApiTestResult.Failed(
                        "Token is null or empty",
                        null,
                        stopwatch.Elapsed);
                }

                if (loginResponse.ExpiresAt <= DateTime.UtcNow)
                {
                    return ApiTestResult.Failed(
                        $"Token expiration date should be in the future, but was {loginResponse.ExpiresAt}",
                        null,
                        stopwatch.Elapsed);
                }

                // Store token for subsequent tests
                _securityToken = loginResponse;
                _token = loginResponse;
                _userId = loginResponse.UserId.ToString();

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that the current user info can be retrieved
        /// </summary>
        [ApiTest("Test getting current user when authenticated", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> GetCurrentUser_WhenAuthenticated_ShouldReturnUserInfo()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Ensure we're authenticated with the token from login test
                if (_token == null)
                {
                    return ApiTestResult.Failed(
                        "Login test should have run first to obtain authentication token",
                        null,
                        stopwatch.Elapsed);
                }

                // Act
                var user = await GetAsync<UserResponse>("identity", "/auth/user");

                // Assert
                stopwatch.Stop();

                if (user == null)
                {
                    return ApiTestResult.Failed(
                        "User response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (user.UserId != _userId)
                {
                    return ApiTestResult.Failed(
                        $"User ID should be {_userId}, but was {user.UserId}",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that user info can be updated
        /// </summary>
        [ApiTest("Test updating user with valid data", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> UpdateUser_WithValidData_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Ensure we're authenticated
                if (_token == null)
                {
                    return ApiTestResult.Failed(
                        "Login test should have run first to obtain authentication token",
                        null,
                        stopwatch.Elapsed);
                }

                var newEmail = $"updated_{Guid.NewGuid():N}@example.com";
                var updateRequest = new UpdateUserRequest
                {
                    Email = newEmail
                };

                // Act
                var updatedUser = await PutAsync<UpdateUserRequest, UserResponse>("identity", "/auth/user", updateRequest);

                // Assert
                stopwatch.Stop();

                if (updatedUser == null)
                {
                    return ApiTestResult.Failed(
                        "Updated user response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (updatedUser.Email != newEmail)
                {
                    return ApiTestResult.Failed(
                        $"Email should be updated to {newEmail}, but was {updatedUser.Email}",
                        null,
                        stopwatch.Elapsed);
                }

                // Update the test email for subsequent tests
                _testEmail = newEmail;

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that a token can be refreshed
        /// </summary>
        [ApiTest("Test refreshing a valid token", Dependencies = new string[] { "SimulationTest.Tests.IdentityServiceTests.Login_WithValidCredentials_ShouldReturnToken" })]
        public async Task<ApiTestResult> RefreshToken_WithValidToken_ShouldReturnNewToken()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Ensure we're authenticated
                if (_token == null)
                {
                    return ApiTestResult.Failed(
                        "Login test should have run first to obtain authentication token",
                        null,
                        stopwatch.Elapsed);
                }

                // Act
                var refreshRequest = new RefreshTokenRequest
                {
                    RefreshToken = _token!.Token
                };

                var newToken = await PostAsync<RefreshTokenRequest, SecurityToken>("identity", "/auth/refresh-token", refreshRequest, false);

                // Assert
                stopwatch.Stop();

                if (newToken == null)
                {
                    return ApiTestResult.Failed(
                        "Refresh token response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (string.IsNullOrWhiteSpace(newToken.Token))
                {
                    return ApiTestResult.Failed(
                        "New token is null or empty",
                        null,
                        stopwatch.Elapsed);
                }

                if (newToken.Token == _token.Token)
                {
                    return ApiTestResult.Failed(
                        "New token should be different from the old token",
                        null,
                        stopwatch.Elapsed);
                }

                if (newToken.ExpiresAt <= DateTime.UtcNow)
                {
                    return ApiTestResult.Failed(
                        $"New token expiration date should be in the future, but was {newToken.ExpiresAt}",
                        null,
                        stopwatch.Elapsed);
                }

                // Update the token for subsequent tests
                _token = newToken;
                _securityToken = newToken;

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }
    }
}