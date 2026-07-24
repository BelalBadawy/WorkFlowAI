using WFAI.Application.Features.Categories.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Categories.Commands.Delete
{
    public record DeleteCategoryCommand(int Id) : IRequest<IResponseWrapper>, IValidateMe;

    public class DeleteCategoryCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<DeleteCategoryCommand, IResponseWrapper>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper> Handle(DeleteCategoryCommand request, CancellationToken ct)
        {
            if (request.Id == 0)
            {
                return ResponseWrapper.Fail("Category Id is required.");
            }

            var category = await _applicationDbContext.Categories.FirstOrDefaultAsync(o => o.Id == request.Id, ct);

            if (category == null)
            {
                return ResponseWrapper.Fail("Category not found.");
            }

            if (await _applicationDbContext.Categories.AnyAsync(o => o.ParentId == request.Id, ct))
            {
                return ResponseWrapper.Fail("Cannot delete category with children.");
            }

            _applicationDbContext.Categories.Remove(category);
            _applicationDbContext.AddOutboxMessage(new CategoryDeletedEvent(request.Id));
            await _applicationDbContext.SaveChangesAsync(ct);

            foreach (var key in CategoryCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper.Success("Category deleted successfully.");
        }
    }
}