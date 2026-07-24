using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;
using WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged;

namespace WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsList
{
    public sealed record GetAuditTrailsListQuery(
        string? TableName,
        string? EntityId,
        string? ActionTypes,
        string? FromDate,
        string? ToDate,
        int? UserId
    ) : IRequest<IResponseWrapper<List<AuditTrailResponse>>>;

    public sealed class GetAuditTrailsListQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetAuditTrailsListQuery, IResponseWrapper<List<AuditTrailResponse>>>
    {
        private readonly IApplicationDbContext _context = context;

        public async ValueTask<IResponseWrapper<List<AuditTrailResponse>>> Handle(GetAuditTrailsListQuery request, CancellationToken ct)
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

            return ResponseWrapper<List<AuditTrailResponse>>.Success(auditTrails);
        }
    }
}