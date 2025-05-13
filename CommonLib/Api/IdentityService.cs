using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommonLib.Models.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonLib.Api
{
    public class IdentityService : BaseService
    {
        public IdentityService(IConfiguration configuration, ILogger? logger = null)
            : base(configuration, "IdentityService", "http://identity.trading-system.local", logger)
        {
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            return await PostAsync<RegisterResponse, RegisterRequest>("/auth/register", request);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            return await PostAsync<LoginResponse, LoginRequest>("/auth/login", request);
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            return await PostAsync<RefreshTokenResponse, RefreshTokenRequest>("/auth/refresh-token", request);
        }

        public async Task<UserResponse> GetCurrentUserAsync(string token)
        {
            return await GetAsync<UserResponse>("/auth/user", token);
        }

        public async Task<UserResponse> UpdateUserAsync(string token, UpdateUserRequest request)
        {
            return await PutAsync<UserResponse, UpdateUserRequest>("/auth/user", request, token);
        }
    }
}