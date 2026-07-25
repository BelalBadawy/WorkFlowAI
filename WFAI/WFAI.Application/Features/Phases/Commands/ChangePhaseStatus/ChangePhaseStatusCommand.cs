using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Phases.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Commands.ChangePhaseStatus
{
    public record ChangePhaseStatusCommand(int Id, bool IsActive) : ICommand<IResponseWrapper<int>>, IValidateMe;

    public class ChangePhaseStatusCommandValidator : AbstractValidator<ChangePhaseStatusCommand>
    {
        public ChangePhaseStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public class ChangePhaseStatusHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
        : ICommandHandler<ChangePhaseStatusCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(ChangePhaseStatusCommand request, CancellationToken ct)
        {
            var phase = await _applicationDbContext.Phases.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
            if (phase == null)
            {
                return ResponseWrapper<int>.Fail("Phase not found.", 404);
            }

            phase.IsActive = request.IsActive;

            _applicationDbContext.Phases.Update(phase);
            _applicationDbContext.AddOutboxMessage(new PhaseUpdatedEvent(phase.Id));
            await _applicationDbContext.SaveChangesAsync(ct);

            foreach (var key in PhaseCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper<int>.Success(phase.Id, request.IsActive 
                ? "Phase activated successfully." 
                : "Phase deactivated successfully.");
        }
    }
}
