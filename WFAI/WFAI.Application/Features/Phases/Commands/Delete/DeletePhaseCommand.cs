using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Phases.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Commands.Delete
{
    public record DeletePhaseCommand(int Id) : IRequest<IResponseWrapper>, IValidateMe;

    public class DeletePhaseCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<DeletePhaseCommand, IResponseWrapper>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper> Handle(DeletePhaseCommand request, CancellationToken ct)
        {
            if (request.Id == 0)
            {
                return ResponseWrapper.Fail("Phase Id is required.");
            }

            var phase = await _applicationDbContext.Phases.FirstOrDefaultAsync(o => o.Id == request.Id, ct);

            if (phase == null)
            {
                return ResponseWrapper.Fail("Phase not found.");
            }

            _applicationDbContext.Phases.Remove(phase);
            _applicationDbContext.AddOutboxMessage(new PhaseDeletedEvent(request.Id));
            await _applicationDbContext.SaveChangesAsync(ct);

            foreach (var key in PhaseCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper.Success("Phase deleted successfully.");
        }
    }
}
