using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Queries.GetAllCategories
{
    public record CategoryListDto(
      int Id,
      string Name,
      string Slug,
      int? ParentId,
      int SortOrder
  );

    public record GetAllCategoriesQuery(bool? isActive) : IRequest<IResponseWrapper<List<CategoryListDto>>>;

    public class GetAllCategoriesQueryHandler(IApplicationDbContext applicationDbContext, ICacheService cacheService)
        : IRequestHandler<GetAllCategoriesQuery, IResponseWrapper<List<CategoryListDto>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;
        public async ValueTask<IResponseWrapper<List<CategoryListDto>>> Handle(GetAllCategoriesQuery request, CancellationToken ct)
        {
            var cacheKey = CategoryCacheKeys.GetAll(request.isActive);

            if (_cacheService.TryGet<List<CategoryListDto>>(cacheKey, out var cachedCategories))
            {
                return ResponseWrapper<List<CategoryListDto>>.Success(data: cachedCategories);
            }

            var categories = await _applicationDbContext.Categories
                .AsNoTracking()
                .Where(x => request.isActive == null ||  x.IsActive == request.isActive)
                .OrderBy(x => x.SortOrder)
                .Select(x => new CategoryListDto(
                                     x.Id,
                                     x.Name,
                                     x.Slug,
                                     x.ParentId,
                                      x.SortOrder
                                  ))
                .ToListAsync(ct);

            _cacheService.Set<List<CategoryListDto>>(cacheKey, categories);

            return ResponseWrapper<List<CategoryListDto>>.Success(categories);
        }
    }
}