using CommonLib.Models.Identity;
using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Implementation of the identity service for authentication and user management
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly IHttpClientService _httpClient;

        /// <summary>
        /// Initializes a new instance of the IdentityService
        /// </summary>
        /// <param name="httpClient">HTTP client service</param>
        public IdentityService(IHttpClientService httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>Registration response with user details and tokens</returns>
        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            return await _httpClient.PostAsync<RegisterRequest, RegisterResponse>("identity", "auth/register", request);
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with authentication tokens</returns>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsync<LoginRequest, LoginResponse>("identity", "auth/login", request);

            // Set the auth token for future requests
            if (!string.IsNullOrEmpty(response.Token))
            {
                _httpClient.SetAuthToken(response.Token);
            }

            return response;
        }

        /// <summary>
        /// Refreshes an authentication token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New security token</returns>
        public async Task<SecurityToken> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var response = await _httpClient.PostAsync<RefreshTokenRequest, SecurityToken>("identity", "auth/refresh-token", request);

            // Set the new auth token for future requests
            if (!string.IsNullOrEmpty(response.Token))
            {
                _httpClient.SetAuthToken(response.Token);
            }

            return response;
        }

        /// <summary>
        /// Gets the current authenticated user
        /// </summary>
        /// <returns>User details</returns>
        public async Task<User> GetCurrentUserAsync()
        {
            return await _httpClient.GetAsync<User>("identity", "auth/user");
        }

        /// <summary>
        /// Updates user information
        /// </summary>
        /// <param name="request">User update information</param>
        /// <returns>Updated user details</returns>
        public async Task<User> UpdateUserAsync(UpdateUserRequest request)
        {
            return await _httpClient.PutAsync<UpdateUserRequest, User>("identity", "auth/user", request);
        }
    }
}
