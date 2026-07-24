using Mediator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;
using WFAI.Application.Features.Categories.Events;
using WFAI.Application.Dtos.Wrappers;

namespace WFAI.Application.Features.Categories.Commands.ChangeCategoryStatus
{
    public record ChangeCategoryStatusCommand(int Id, bool IsActive) : ICommand<IResponseWrapper<int>>, IValidateMe;

    public class ChangeCategoryStatusCommandValidator : AbstractValidator<ChangeCategoryStatusCommand>
    {
        public ChangeCategoryStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public class ChangeCategoryStatusHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
        : ICommandHandler<ChangeCategoryStatusCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(ChangeCategoryStatusCommand request, CancellationToken ct)
        {
            var category = await _applicationDbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
            if (category == null)
            {
                return ResponseWrapper<int>.Fail("Category not found.", 404);
            }

            category.IsActive = request.IsActive;

            _applicationDbContext.Categories.Update(category);
            _applicationDbContext.AddOutboxMessage(new CategoryUpdatedEvent(category.Id));
            await _applicationDbContext.SaveChangesAsync(ct);

            foreach (var key in CategoryCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper<int>.Success(category.Id, request.IsActive 
                ? "Category activated successfully." 
                : "Category deactivated successfully.");
        }
    }
}