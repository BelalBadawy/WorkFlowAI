using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Queries.GetCategoriesPagedAdmin
{
    public record CategoryAdminResponse(
        int Id,
        string Name,
        string Slug,
        int? ParentId,
        string? ParentName,
        bool IsActive,
        int SortOrder
    );

    public class GetCategoriesPagedAdminQuery : IRequest<IResponseWrapper<PagedResult<CategoryAdminResponse>>>, IValidateMe
    {
        public PagedFilterRequest PagedFilterRequest { get; set; } = new();
    }

    public class GetCategoriesPagedAdminQueryHandler(IApplicationDbContext applicationDbContext)
        : IRequestHandler<GetCategoriesPagedAdminQuery, IResponseWrapper<PagedResult<CategoryAdminResponse>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;

        public async ValueTask<IResponseWrapper<PagedResult<CategoryAdminResponse>>> Handle(GetCategoriesPagedAdminQuery request, CancellationToken ct)
        {
            var pagedFilter = request.PagedFilterRequest;
            // Admin listing intentionally honors global soft-delete filters.
            var categoriesQuery = _applicationDbContext.Categories.AsNoTracking();

            // 1. Filtering
            if (!string.IsNullOrWhiteSpace(pagedFilter.SearchTerm))
            {
                var term = pagedFilter.SearchTerm.Trim();
                var pattern = $"%{term}%";
                categoriesQuery = categoriesQuery.Where(c =>
                    EF.Functions.Like(c.Name, pattern) ||
                    EF.Functions.Like(c.Slug, pattern));
            }

            if (pagedFilter.IsActive.HasValue)
            {
                categoriesQuery = categoriesQuery.Where(c => c.IsActive == pagedFilter.IsActive.Value);
            }

            // 2. Sorting
            categoriesQuery = pagedFilter.SortBy?.ToLower() switch
            {
                "name" => pagedFilter.SortDirection == "desc"
                    ? categoriesQuery.OrderByDescending(c => c.Name)
                    : categoriesQuery.OrderBy(c => c.Name),
                "slug" => pagedFilter.SortDirection == "desc"
                    ? categoriesQuery.OrderByDescending(c => c.Slug)
                    : categoriesQuery.OrderBy(c => c.Slug),
                "sortorder" => pagedFilter.SortDirection == "desc"
                    ? categoriesQuery.OrderByDescending(c => c.SortOrder)
                    : categoriesQuery.OrderBy(c => c.SortOrder),
                "id" => pagedFilter.SortDirection == "desc"
                    ? categoriesQuery.OrderByDescending(c => c.Id)
                    : categoriesQuery.OrderBy(c => c.Id),
                _ => pagedFilter.SortDirection == "desc"
                    ? categoriesQuery.OrderByDescending(c => c.SortOrder).ThenBy(c => c.Name)
                    : categoriesQuery.OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            };

            // 3. Pagination
            var totalCount = await categoriesQuery.CountAsync(ct);

            var categories = await categoriesQuery
                .Skip((pagedFilter.PageNumber - 1) * pagedFilter.PageSize)
                .Take(pagedFilter.PageSize)
                .Select(c => new CategoryAdminResponse(
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.ParentId,
                    c.Parent != null ? c.Parent.Name : null,
                    c.IsActive,
                    c.SortOrder
                ))
                .ToListAsync(ct);

            var pagedResult = PagedResult<CategoryAdminResponse>.Create(
                categories,
                totalCount,
                pagedFilter.PageNumber,
                pagedFilter.PageSize);

            return ResponseWrapper<PagedResult<CategoryAdminResponse>>.Success(pagedResult);
        }
    }
}