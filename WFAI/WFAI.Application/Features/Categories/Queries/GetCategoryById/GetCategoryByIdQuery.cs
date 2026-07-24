namespace WFAI.Application.Features.Categories.Queries.GetCategoryById
{
    public record CategoryDto(
       int Id,
       string Name,
       string Slug,
       string? ParentName
   );

    public record GetCategoryByIdQuery(int Id) : IRequest<IResponseWrapper<CategoryDto>>, IValidateMe;

    public class GetCategoryByIdQueryHandler(IApplicationDbContext applicationDbContext)
        : IRequestHandler<GetCategoryByIdQuery, IResponseWrapper<CategoryDto>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;

        public async ValueTask<IResponseWrapper<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken ct)
        {
            var categoryDto = await _applicationDbContext.Categories
                                 .AsNoTracking()
                                 .Where(x => x.Id == request.Id && x.IsActive)
                                 .Select(x => new CategoryDto(
                                     x.Id,
                                     x.Name,
                                     x.Slug,
                                     x.Parent != null ? x.Parent.Name : null
                                  ))
                                 .FirstOrDefaultAsync(ct);

            if (categoryDto == null)
            {
                return ResponseWrapper<CategoryDto>.Fail("Category not found.");
            }

            return ResponseWrapper<CategoryDto>.Success(categoryDto);
        }
    }
}