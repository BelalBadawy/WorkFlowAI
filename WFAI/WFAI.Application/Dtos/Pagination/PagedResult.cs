namespace WFAI.Application.Dtos.Pagination
{
    /// <summary>
    /// Represents a single page of results with pagination metadata.
    /// </summary>
    /// <typeparam name="T">Type of items contained in the page.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items contained in the current page.
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// Current page number (1-based).
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages calculated from <see cref="TotalCount"/> and <see cref="PageSize"/>.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Indicates whether there is a page before the current page.
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Indicates whether there is a page after the current page.
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Factory method to create a <see cref="PagedResult{T}"/> instance.
        /// </summary>
        /// <param name="data">Items for the current page.</param>
        /// <param name="totalCount">Total number of items available.</param>
        /// <param name="pageNumber">Current page number (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>A populated <see cref="PagedResult{T}"/> instance.</returns>
        public static PagedResult<T> Create(List<T> data, int totalCount, int pageNumber, int pageSize)
        {
            return new PagedResult<T>
            {
                Data = data,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }
    }
}