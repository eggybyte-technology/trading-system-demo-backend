using System.Threading.Tasks;

namespace SimulationTest.Services
{
    /// <summary>
    /// Interface for making HTTP requests to the API endpoints
    /// </summary>
    public interface IHttpClientService
    {
        /// <summary>
        /// Sets the authentication token for API requests
        /// </summary>
        /// <param name="token">JWT token</param>
        void SetAuthToken(string token);

        /// <summary>
        /// Makes a GET request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>Response object</returns>
        Task<TResponse> GetAsync<TResponse>(string service, string endpoint, string queryParams = null);

        /// <summary>
        /// Makes a POST request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="request">Request object</param>
        /// <returns>Response object</returns>
        Task<TResponse> PostAsync<TRequest, TResponse>(string service, string endpoint, TRequest request);

        /// <summary>
        /// Makes a PUT request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="request">Request object</param>
        /// <returns>Response object</returns>
        Task<TResponse> PutAsync<TRequest, TResponse>(string service, string endpoint, TRequest request);

        /// <summary>
        /// Makes a DELETE request to the specified service and endpoint
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="service">Service name</param>
        /// <param name="endpoint">API endpoint</param>
        /// <returns>Response object</returns>
        Task<TResponse> DeleteAsync<TResponse>(string service, string endpoint);
    }
}