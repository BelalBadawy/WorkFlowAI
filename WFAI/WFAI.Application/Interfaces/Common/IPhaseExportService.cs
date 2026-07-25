using WFAI.Application.Features.Phases;

namespace WFAI.Application.Interfaces.Common
{
    public interface IPhaseExportService
    {
        Task<byte[]> ExportPhasesAsync(List<PhaseDto> data, string format, CancellationToken ct);
    }
}
