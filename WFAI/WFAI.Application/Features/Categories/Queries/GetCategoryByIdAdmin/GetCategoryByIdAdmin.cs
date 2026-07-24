namespace WFAI.Application.Features.Categories.Queries.GetCategoryByIdAdmin
{

    public record CategoryAdminDto(
       int Id,
       string Name,
       string Slug,
       int? ParentId,
       string? ParentName,
       bool IsActive,
       byte[] RowVersion,
       int SortOrder
   );


    public record GetCategoryByIdAdminQuery(int Id) : IRequest<IResponseWrapper<CategoryAdminDto>>, IValidateMe;

    public class GetCategoryByIdAdminQueryHandler(IApplicationDbContext applicationDbContext)
        : IRequestHandler<GetCategoryByIdAdminQuery, IResponseWrapper<CategoryAdminDto>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;

        public async ValueTask<IResponseWrapper<CategoryAdminDto>> Handle(GetCategoryByIdAdminQuery request, CancellationToken ct)
        {
            // Admin details intentionally honor global soft-delete filters.
            var categoryDto = await _applicationDbContext.Categories
                                 .AsNoTracking()
                                 .Where(x => x.Id == request.Id)
                                 .Select(x => new CategoryAdminDto(
                                     x.Id,
                                     x.Name,
                                     x.Slug,
                                     x.ParentId,
                                     x.Parent != null ? x.Parent.Name : null,
                                     x.IsActive,
                                     x.RowVersion,
                                     x.SortOrder
                                 ))
                                 .FirstOrDefaultAsync(ct);

            if (categoryDto == null)
            {
                return ResponseWrapper<CategoryAdminDto>.Fail("Category not found or has been deleted.");
            }

            return ResponseWrapper<CategoryAdminDto>.Success(categoryDto);
        }
    }
}