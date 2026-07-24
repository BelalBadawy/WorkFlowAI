using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Categories.Events;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Categories.Commands.RestoreCategory
{
    public sealed record RestoreCategoryCommand(int Id) : IRequest<IResponseWrapper<int>>, IValidateMe;

    public sealed class RestoreCategoryCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
        : IRequestHandler<RestoreCategoryCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(RestoreCategoryCommand request, CancellationToken ct)
        {
            var category = await _applicationDbContext.Categories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

            if (category == null)
            {
                return ResponseWrapper<int>.Fail("Category not found.", 404);
            }

            // Check if there is an active (non-deleted) category that has the same Name or Slug as the deleted category being restored.
            var conflictExists = await _applicationDbContext.Categories
                .AnyAsync(c => !c.SoftDeleted && (c.NormalizedName == category.NormalizedName || c.NormalizedSlug == category.NormalizedSlug), ct);

            if (conflictExists)
            {
                return ResponseWrapper<int>.Fail("Cannot restore: An active category with the same name or slug already exists.", 409);
            }

            if (category.SoftDeleted)
            {
                category.SoftDeleted = false;
                category.DeletedAt = null;
                category.DeletedBy = null;

                _applicationDbContext.AddOutboxMessage(new CategoryRestoredEvent(category.Id));
                await _applicationDbContext.SaveChangesAsync(ct);

                foreach (var key in CategoryCacheKeys.All)
                {
                    _cacheService.Remove(key);
                }
            }

            return ResponseWrapper<int>.Success(category.Id, "Category restored successfully.");
        }
    }
}