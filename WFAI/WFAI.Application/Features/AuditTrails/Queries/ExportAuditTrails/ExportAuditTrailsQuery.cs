using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.AuditTrails.Queries.ExportAuditTrails
{
    public sealed record ExportAuditTrailsQuery : IRequest<IResponseWrapper<byte[]>>
    {
        public string? TableName { get; init; }
        public string? EntityId { get; init; }
        public string? ActionTypes { get; init; }
        public string? FromDate { get; init; }
        public string? ToDate { get; init; }
        public int? UserId { get; init; }
        public string ExportFormat { get; init; } = "excel";
    }

    public sealed class ExportAuditTrailsQueryHandler(
        IApplicationDbContext context,
        IAuditTrailExportService auditTrailExportService)
        : IRequestHandler<ExportAuditTrailsQuery, IResponseWrapper<byte[]>>
    {
        private readonly IApplicationDbContext _context = context;
        private readonly IAuditTrailExportService _auditTrailExportService = auditTrailExportService;

        public async ValueTask<IResponseWrapper<byte[]>> Handle(ExportAuditTrailsQuery request, CancellationToken ct)
        {
            var auditQuery = from audit in _context.AuditTrails.AsNoTracking()
                             join user in _context.Users.AsNoTracking() on audit.UserId equals user.Id into userGroup
                             from user in userGroup.DefaultIfEmpty()
                             select new AuditTrailQueryModel { Audit = audit, UserEmail = user != null ? user.Email : null };

            auditQuery = auditQuery
                .ApplyAuditTrailFilters(
                    request.UserId,
                    request.TableName,
                    request.EntityId,
                    request.ActionTypes,
                    request.FromDate,
                    request.ToDate)
                .ApplyAuditTrailSorting(null, null);

            var auditTrails = await auditQuery
                .Select(a => new AuditTrailResponse(
                    a.Audit.Id,
                    a.Audit.UserId,
                    a.UserEmail,
                    a.Audit.IpAddress,
                    a.Audit.Type.ToString(),
                    a.Audit.TableName,
                    a.Audit.DateTime,
                    a.Audit.OldValues,
                    a.Audit.NewValues,
                    a.Audit.AffectedColumns,
                    a.Audit.PrimaryKey
                ))
                .ToListAsync(ct);

            var fileBytes = await _auditTrailExportService.ExportAuditTrailsAsync(auditTrails, request.ExportFormat, ct);

            return ResponseWrapper<byte[]>.Success(fileBytes);
        }
    }
}