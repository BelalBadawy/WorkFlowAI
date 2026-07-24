using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged;

namespace WFAI.Application.Interfaces.Common
{
    public interface IAuditTrailExportService
    {
        Task<byte[]> ExportAuditTrailsAsync(List<AuditTrailResponse> data, string format, CancellationToken ct);
    }
}