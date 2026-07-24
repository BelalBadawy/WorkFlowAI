using WFAI.Application.Features.Categories.Commands;
using WFAI.Application.Features.Categories.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Commands.Update
{
    public record UpdateCategoryCommand(
        int Id,
        string Name,
        string Slug,
        int? ParentId,
        bool IsActive,
        int SortOrder,
        byte[] RowVersion
    ) : IRequest<IResponseWrapper>, IValidateMe;

    public class UpdateCategoryCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<UpdateCategoryCommand, IResponseWrapper>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper> Handle(UpdateCategoryCommand request, CancellationToken ct)
        {
            var normalizedName = CategoryWriteGuards.NormalizeKey(request.Name);
            var normalizedSlug = CategoryWriteGuards.NormalizeKey(request.Slug);

            var category = await _applicationDbContext.Categories.FirstOrDefaultAsync(o => o.Id == request.Id, ct);

            if (category == null)
            {
                return ResponseWrapper.Fail("Category not found.");
            }

            var parentValidationError = await CategoryWriteGuards.ValidateParentAssignmentAsync(
                _applicationDbContext,
                request.Id,
                request.ParentId,
                ct);

            if (!string.IsNullOrWhiteSpace(parentValidationError))
            {
                return ResponseWrapper.Fail(parentValidationError);
            }

            if (await _applicationDbContext.Categories.AnyAsync(
                    o => o.NormalizedName == normalizedName && o.Id != request.Id,
                    ct))
            {
                return ResponseWrapper.Fail("Category with this name already exists.");
            }

            if (await _applicationDbContext.Categories.AnyAsync(
                    o => o.NormalizedSlug == normalizedSlug && o.Id != request.Id,
                    ct))
            {
                return ResponseWrapper.Fail("Category with this slug already exists.");
            }

            _applicationDbContext.SetOriginalRowVersion(category, request.RowVersion);

            category.Name = request.Name.Trim();
            category.NormalizedName = normalizedName;
            category.Slug = request.Slug.Trim();
            category.NormalizedSlug = normalizedSlug;
            category.ParentId = request.ParentId;
            category.IsActive = request.IsActive;
            category.SortOrder = request.SortOrder;

            try
            {
                _applicationDbContext.Categories.Update(category);
                _applicationDbContext.AddOutboxMessage(new CategoryUpdatedEvent(request.Id));
                await _applicationDbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResponseWrapper.Fail(
                    "Concurrency conflict: this category was modified by another user. Refresh and try again.",
                    409);
            }
            catch (DbUpdateException ex) when (CategoryWriteGuards.IsUniqueConstraintViolation(ex))
            {
                return ResponseWrapper.Fail(CategoryWriteGuards.GetUniqueConstraintMessage(ex));
            }

            foreach (var key in CategoryCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper.Success("Category updated successfully.");
        }
    }
}