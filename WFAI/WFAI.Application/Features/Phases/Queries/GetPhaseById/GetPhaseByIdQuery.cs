using Mediator;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.Phases.Queries.GetPhaseById
{
    public record GetPhaseByIdQuery(int Id) : IRequest<IResponseWrapper<PhaseDto>>, IValidateMe;

    public class GetPhaseByIdQueryHandler(IApplicationDbContext applicationDbContext)
        : IRequestHandler<GetPhaseByIdQuery, IResponseWrapper<PhaseDto>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;

        public async ValueTask<IResponseWrapper<PhaseDto>> Handle(GetPhaseByIdQuery request, CancellationToken ct)
        {
            var phaseDto = await _applicationDbContext.Phases
                .AsNoTracking()
                .Where(x => x.Id == request.Id && x.IsActive)
                .Select(x => new PhaseDto(
                    x.Id,
                    x.Title,
                    x.Description,
                    x.SortOrder,
                    x.IsActive,
                    x.SoftDeleted,
                    x.RowVersion
                ))
                .FirstOrDefaultAsync(ct);

            if (phaseDto == null)
            {
                return ResponseWrapper<PhaseDto>.Fail("Phase not found.");
            }

            return ResponseWrapper<PhaseDto>.Success(phaseDto);
        }
    }
}
