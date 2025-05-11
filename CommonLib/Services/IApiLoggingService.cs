using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace CommonLib.Services
{
    /// <summary>
    /// Interface for API logging service
    /// </summary>
    public interface IApiLoggingService
    {
        /// <summary>
        /// Logs an API request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task LogApiRequest(HttpContext context);

        /// <summary>
        /// Logs an API response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="responseBody">Response body</param>
        /// <param name="responseTime">Response time in milliseconds</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task LogApiResponse(HttpContext context, string responseBody, long responseTime);

        /// <summary>
        /// Logs an API call to another service
        /// </summary>
        /// <typeparam name="T">The return type of the API call</typeparam>
        /// <param name="servicePrefix">The service prefix</param>
        /// <param name="context">HTTP context</param>
        /// <param name="apiCall">The API call to execute</param>
        /// <returns>The result of the API call</returns>
        Task<T> LogApiCallAsync<T>(string servicePrefix, HttpContext context, Func<Task<T>> apiCall);
    }
}