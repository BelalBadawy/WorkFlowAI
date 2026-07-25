using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Queries.GetPhasesPaged
{
    public sealed record GetPhasesPagedQuery : IRequest<IResponseWrapper<PagedResult<PhaseDto>>>, IValidateMe
    {
        public PagedFilterRequest PagedFilterRequest { get; init; } = new();
        public bool IncludeDeleted { get; init; } = false;
    }

    public sealed class GetPhasesPagedQueryHandler(
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService)
        : IRequestHandler<GetPhasesPagedQuery, IResponseWrapper<PagedResult<PhaseDto>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async ValueTask<IResponseWrapper<PagedResult<PhaseDto>>> Handle(GetPhasesPagedQuery request, CancellationToken ct)
        {
            var pagedFilterRequest = request.PagedFilterRequest;
            var query = _applicationDbContext.Phases
                .AsNoTracking()
                .ApplyPhaseFilters(_currentUserService, pagedFilterRequest.SearchTerm, pagedFilterRequest.IsActive, request.IncludeDeleted)
                .ApplyPhaseSorting(pagedFilterRequest.SortBy, pagedFilterRequest.SortDirection);

            var totalCount = await query.CountAsync(ct);

            var phases = await query
                .Skip((pagedFilterRequest.PageNumber - 1) * pagedFilterRequest.PageSize)
                .Take(pagedFilterRequest.PageSize)
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

            var pagedResult = PagedResult<PhaseDto>.Create(
                phases,
                totalCount,
                pagedFilterRequest.PageNumber,
                pagedFilterRequest.PageSize);

            return ResponseWrapper<PagedResult<PhaseDto>>.Success(pagedResult);
        }
    }
}
