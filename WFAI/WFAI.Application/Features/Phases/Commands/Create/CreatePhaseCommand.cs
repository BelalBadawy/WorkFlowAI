using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Phases.Commands;
using WFAI.Application.Features.Phases.Events;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Phases.Commands.Create
{
    public class CreatePhaseRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public record CreatePhaseCommand(
        string Title,
        string? Description,
        bool IsActive,
        int SortOrder
    ) : IRequest<IResponseWrapper<int>>, IValidateMe;

    public class CreatePhaseCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<CreatePhaseCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(CreatePhaseCommand request, CancellationToken ct)
        {
            var normalizedTitle = PhaseWriteGuards.NormalizeKey(request.Title);

            if (await _applicationDbContext.Phases.AnyAsync(
                    o => o.NormalizedTitle == normalizedTitle,
                    ct))
            {
                return ResponseWrapper<int>.Fail("Phase with this title already exists.");
            }

            var phase = new Phase
            {
                Title = request.Title.Trim(),
                NormalizedTitle = normalizedTitle,
                Description = request.Description?.Trim(),
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                RowVersion = [0]
            };

            try
            {
                await _applicationDbContext.Phases.AddAsync(phase, ct);
                await _applicationDbContext.SaveChangesAsync(ct);

                _applicationDbContext.AddOutboxMessage(new PhaseCreatedEvent(phase.Id));
                await _applicationDbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (PhaseWriteGuards.IsUniqueConstraintViolation(ex))
            {
                return ResponseWrapper<int>.Fail(PhaseWriteGuards.GetUniqueConstraintMessage(ex));
            }

            foreach (var key in PhaseCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper<int>.Success(phase.Id, "Phase created successfully.");
        }
    }
}
