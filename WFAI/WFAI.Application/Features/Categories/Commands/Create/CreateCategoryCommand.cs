using WFAI.Application.Features.Categories.Commands;
using WFAI.Application.Features.Categories.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Commands.Create
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public record CreateCategoryCommand(
        string Name,
        string Slug,
        int? ParentId,
        bool IsActive,
        int SortOrder
    ) : IRequest<IResponseWrapper<int>>, IValidateMe;

    public class CreateCategoryCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<CreateCategoryCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(CreateCategoryCommand request, CancellationToken ct)
        {
            var normalizedName = CategoryWriteGuards.NormalizeKey(request.Name);
            var normalizedSlug = CategoryWriteGuards.NormalizeKey(request.Slug);

            var parentValidationError = await CategoryWriteGuards.ValidateParentAssignmentAsync(
                _applicationDbContext,
                categoryId: null,
                parentId: request.ParentId,
                ct);

            if (!string.IsNullOrWhiteSpace(parentValidationError))
            {
                return ResponseWrapper<int>.Fail(parentValidationError);
            }

            if (await _applicationDbContext.Categories.AnyAsync(
                    o => o.NormalizedName == normalizedName,
                    ct))
            {
                return ResponseWrapper<int>.Fail("Category with this name already exists.");
            }

            if (await _applicationDbContext.Categories.AnyAsync(
                    o => o.NormalizedSlug == normalizedSlug,
                    ct))
            {
                return ResponseWrapper<int>.Fail("Category with this slug already exists.");
            }

            var category = new Category
            {
                Name = request.Name.Trim(),
                NormalizedName = normalizedName,
                Slug = request.Slug.Trim(),
                NormalizedSlug = normalizedSlug,
                ParentId = request.ParentId,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                RowVersion = [0]
            };

            try
            {
                await _applicationDbContext.Categories.AddAsync(category, ct);
                await _applicationDbContext.SaveChangesAsync(ct);

                _applicationDbContext.AddOutboxMessage(new CategoryCreatedEvent(category.Id));
                await _applicationDbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (CategoryWriteGuards.IsUniqueConstraintViolation(ex))
            {
                return ResponseWrapper<int>.Fail(CategoryWriteGuards.GetUniqueConstraintMessage(ex));
            }

            foreach (var key in CategoryCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper<int>.Success(category.Id, "Category created successfully.");
        }
    }
}