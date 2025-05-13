using CommonLib.Models.Identity;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for identity operations (authentication, user management)
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>Registration response with user details and tokens</returns>
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with authentication tokens</returns>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Refreshes an authentication token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New security token</returns>
        Task<SecurityToken> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>
        /// Gets the current authenticated user
        /// </summary>
        /// <returns>User details</returns>
        Task<User> GetCurrentUserAsync();

        /// <summary>
        /// Updates user information
        /// </summary>
        /// <param name="request">User update information</param>
        /// <returns>Updated user details</returns>
        Task<User> UpdateUserAsync(UpdateUserRequest request);
    }
}