using System;

namespace CommonLib.Models
{
    /// <summary>
    /// Generic API response wrapper for all API responses
    /// </summary>
    /// <typeparam name="T">Type of data in the response</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Response data
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message, if any
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error code, if any
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}