namespace WFAI.Application.Dtos.Pagination
{
    /// <summary>
    /// Request model for paged queries. Encapsulates paging, filtering and sorting
    /// parameters sent from clients.
    /// </summary>
    public class PagedFilterRequest
    {
        /// <summary>
        /// The requested page number (1-based). Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of items per page. Defaults to 10.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Optional search term used to filter results.
        /// </summary>
        public string? SearchTerm { get; set; }          // optional search filter

        /// <summary>
        /// Optional field name to sort by.
        /// </summary>
        public string? SortBy { get; set; } = ""; // field name

        /// <summary>
        /// Sort direction: "asc" or "desc". Defaults to "asc".
        /// </summary>
        public string? SortDirection { get; set; } = "asc"; // asc or desc

        /// <summary>
        /// Optional filter for active status.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Optional filter for locked status.
        /// </summary>
        public bool? IsLocked { get; set; }

        /// <summary>
        /// Optional filter for role ID.
        /// </summary>
        public int? RoleId { get; set; }
    }
}