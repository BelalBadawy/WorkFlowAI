using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Queries.GetCategoriesAdmin
{
    public record CategoryListAdminDto(
        int Id,
        string Name,
        string Slug,
        int? ParentId,
        // string? ParentName,
        bool IsActive,
        int SortOrder
    );

    public record GetAllCategoriesAdminQuery : IRequest<IResponseWrapper<List<CategoryListAdminDto>>>;

    public class GetAllCategoriesAdminQueryHandler(IApplicationDbContext applicationDbContext, ICacheService cacheService)
        : IRequestHandler<GetAllCategoriesAdminQuery, IResponseWrapper<List<CategoryListAdminDto>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<List<CategoryListAdminDto>>> Handle(GetAllCategoriesAdminQuery request, CancellationToken ct)
        {
            if (_cacheService.TryGet<List<CategoryListAdminDto>>(CategoryCacheKeys.GetAllAdmin, out var cachedCategories))
            {
                return ResponseWrapper<List<CategoryListAdminDto>>.Success(data: cachedCategories);
            }

            // Admin listing intentionally honors global soft-delete filters.
            var categories = await _applicationDbContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryListAdminDto(
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.ParentId,
                    c.IsActive,
                    c.SortOrder
                ))
                .ToListAsync(ct);

            _cacheService.Set<List<CategoryListAdminDto>>(CategoryCacheKeys.GetAllAdmin, categories);

            return ResponseWrapper<List<CategoryListAdminDto>>.Success(categories);
        }
    }
}