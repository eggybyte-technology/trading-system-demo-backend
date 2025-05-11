using System.Collections.Generic;

namespace CommonLib.Models
{
    /// <summary>
    /// Represents a paginated result from API calls
    /// </summary>
    /// <typeparam name="T">Type of items in the result</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Gets or sets the items on the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the current page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the total number of items across all pages
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets the total number of pages
        /// </summary>
        public int TotalPages => PageSize > 0 ? (TotalItems + PageSize - 1) / PageSize : 0;

        /// <summary>
        /// Gets whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Gets whether there is a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;
    }
}