using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Queries.ExportPhases
{
    public sealed record ExportPhasesQuery : IRequest<IResponseWrapper<byte[]>>
    {
        public string? SearchTerm { get; init; }
        public bool? IsActive { get; init; }
        public string? SortBy { get; init; }
        public string? SortDirection { get; init; }
        public string ExportFormat { get; init; } = "excel";
        public bool IncludeDeleted { get; init; } = false;
    }

    public sealed class ExportPhasesQueryHandler(
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        IPhaseExportService phaseExportService)
        : IRequestHandler<ExportPhasesQuery, IResponseWrapper<byte[]>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IPhaseExportService _phaseExportService = phaseExportService;

        public async ValueTask<IResponseWrapper<byte[]>> Handle(ExportPhasesQuery request, CancellationToken ct)
        {
            var phases = await _applicationDbContext.Phases
                .AsNoTracking()
                .ApplyPhaseFilters(_currentUserService, request.SearchTerm, request.IsActive, request.IncludeDeleted)
                .ApplyPhaseSorting(request.SortBy, request.SortDirection)
                .Select(c => new PhaseDto(
                    c.Id,
                    c.Title,
                    c.Description,
                    c.SortOrder,
                    c.IsActive,
                    c.SoftDeleted,
                    c.RowVersion
                ))
                .ToListAsync(ct);

            var fileBytes = await _phaseExportService.ExportPhasesAsync(phases, request.ExportFormat, ct);

            return ResponseWrapper<byte[]>.Success(fileBytes);
        }
    }
}
