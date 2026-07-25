using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Phases.Commands;
using WFAI.Application.Features.Phases.Events;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Commands.Update
{
    public class UpdatePhaseRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public record UpdatePhaseCommand(
        int Id,
        string Title,
        string? Description,
        bool IsActive,
        int SortOrder,
        byte[] RowVersion
    ) : IRequest<IResponseWrapper>, IValidateMe;

    public class UpdatePhaseCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<UpdatePhaseCommand, IResponseWrapper>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper> Handle(UpdatePhaseCommand request, CancellationToken ct)
        {
            var normalizedTitle = PhaseWriteGuards.NormalizeKey(request.Title);

            var phase = await _applicationDbContext.Phases.FirstOrDefaultAsync(o => o.Id == request.Id, ct);

            if (phase == null)
            {
                return ResponseWrapper.Fail("Phase not found.");
            }

            if (await _applicationDbContext.Phases.AnyAsync(
                    o => o.NormalizedTitle == normalizedTitle && o.Id != request.Id,
                    ct))
            {
                return ResponseWrapper.Fail("Phase with this title already exists.");
            }

            _applicationDbContext.SetOriginalRowVersion(phase, request.RowVersion);

            phase.Title = request.Title.Trim();
            phase.NormalizedTitle = normalizedTitle;
            phase.Description = request.Description?.Trim();
            phase.IsActive = request.IsActive;
            phase.SortOrder = request.SortOrder;

            try
            {
                await _applicationDbContext.SaveChangesAsync(ct);
                _applicationDbContext.AddOutboxMessage(new PhaseUpdatedEvent(phase.Id));
                await _applicationDbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResponseWrapper.Fail("The phase was modified by another user. Please reload and try again.");
            }
            catch (DbUpdateException ex) when (PhaseWriteGuards.IsUniqueConstraintViolation(ex))
            {
                return ResponseWrapper.Fail(PhaseWriteGuards.GetUniqueConstraintMessage(ex));
            }

            foreach (var key in PhaseCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper.Success("Phase updated successfully.");
        }
    }
}
