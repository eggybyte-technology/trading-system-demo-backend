namespace CommonLib.Api
{
    /// <summary>
    /// Standard API response wrapper class matching backend API response format
    /// </summary>
    /// <typeparam name="T">The type of data contained in the response</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Response data (successful responses only)
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message (failed responses only)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error code (failed responses only)
        /// </summary>
        public string? Code { get; set; }
    }
}