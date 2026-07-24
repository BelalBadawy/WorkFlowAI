using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Queries.GetAllCategoriesForList
{
    public record GetAllCategoriesForListQuery : IRequest<IResponseWrapper<List<CategoryLookupDto>>>;

    public class GetAllCategoriesForListQueryHandler(IApplicationDbContext applicationDbContext, ICacheService cacheService) : IRequestHandler<GetAllCategoriesForListQuery, IResponseWrapper<List<CategoryLookupDto>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;
        public async ValueTask<IResponseWrapper<List<CategoryLookupDto>>> Handle(GetAllCategoriesForListQuery request, CancellationToken cancellationToken)
        {

            if (_cacheService.TryGet<List<CategoryLookupDto>>(CategoryCacheKeys.GetAllForList, out var cachedCategories))
            {
                return ResponseWrapper<List<CategoryLookupDto>>.Success(data: cachedCategories);
            }

            var categories = await _applicationDbContext.Categories
                  .AsNoTracking()
                  .Where(c => c.IsActive)
                  .OrderBy(c => c.SortOrder)
                  .ThenBy(c => c.Name)
                  .Select(c => new CategoryLookupDto(
                      c.Id,
                      c.Name
                  ))
                  .ToListAsync(cancellationToken);

            _cacheService.Set<List<CategoryLookupDto>>(CategoryCacheKeys.GetAllForList, categories);

            return ResponseWrapper<List<CategoryLookupDto>>.Success(categories);
        }
    }
}