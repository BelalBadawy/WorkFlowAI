using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Phases.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Commands.RestorePhase
{
    public sealed record RestorePhaseCommand(int Id) : IRequest<IResponseWrapper<int>>, IValidateMe;

    public sealed class RestorePhaseCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
        : IRequestHandler<RestorePhaseCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(RestorePhaseCommand request, CancellationToken ct)
        {
            var phase = await _applicationDbContext.Phases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

            if (phase == null)
            {
                return ResponseWrapper<int>.Fail("Phase not found.", 404);
            }

            var conflictExists = await _applicationDbContext.Phases
                .AnyAsync(c => !c.SoftDeleted && c.NormalizedTitle == phase.NormalizedTitle, ct);

            if (conflictExists)
            {
                return ResponseWrapper<int>.Fail("Cannot restore: An active phase with the same title already exists.", 409);
            }

            if (phase.SoftDeleted)
            {
                phase.SoftDeleted = false;
                phase.DeletedAt = null;
                phase.DeletedBy = null;

                _applicationDbContext.AddOutboxMessage(new PhaseUpdatedEvent(phase.Id));
                await _applicationDbContext.SaveChangesAsync(ct);

                foreach (var key in PhaseCacheKeys.All)
                {
                    _cacheService.Remove(key);
                }
            }

            return ResponseWrapper<int>.Success(phase.Id, "Phase restored successfully.");
        }
    }
}
