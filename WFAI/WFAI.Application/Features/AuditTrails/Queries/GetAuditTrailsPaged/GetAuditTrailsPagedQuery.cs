using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged
{
    public record AuditTrailResponse(
        int Id,
        int? UserId,
        string? UserEmail,
        string? IpAddress,
        string Type,
        string? TableName,
        DateTime DateTime,
        string? OldValues,
        string? NewValues,
        string? AffectedColumns,
        string? PrimaryKey
    );

    public sealed record GetAuditTrailsPagedQuery : IRequest<IResponseWrapper<PagedResult<AuditTrailResponse>>>, IValidateMe
    {
        public PagedFilterRequest PagedFilterRequest { get; init; } = new();
        public string? TableName { get; init; }
        public string? EntityId { get; init; }
        public string? ActionTypes { get; init; }
        public string? FromDate { get; init; }
        public string? ToDate { get; init; }
        public int? UserId { get; init; }
    }

    public sealed class GetAuditTrailsPagedQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetAuditTrailsPagedQuery, IResponseWrapper<PagedResult<AuditTrailResponse>>>
    {
        private readonly IApplicationDbContext _context = context;

        public async ValueTask<IResponseWrapper<PagedResult<AuditTrailResponse>>> Handle(GetAuditTrailsPagedQuery request, CancellationToken ct)
        {
            var pagedFilterRequest = request.PagedFilterRequest;

            var auditQuery = from audit in _context.AuditTrails.AsNoTracking()
                             join user in _context.Users.AsNoTracking() on audit.UserId equals user.Id into userGroup
                             from user in userGroup.DefaultIfEmpty()
                             select new AuditTrailQueryModel { Audit = audit, UserEmail = user != null ? user.Email : null };

            auditQuery = auditQuery.ApplyAuditTrailFilters(
                request.UserId,
                request.TableName,
                request.EntityId,
                request.ActionTypes,
                request.FromDate,
                request.ToDate);

            // Apply SearchTerm filter
            if (!string.IsNullOrWhiteSpace(pagedFilterRequest.SearchTerm))
            {
                var term = pagedFilterRequest.SearchTerm.Trim();
                var pattern = $"%{term}%";

                auditQuery = auditQuery.Where(a =>
                    EF.Functions.Like(a.Audit.TableName ?? "", pattern) ||
                    EF.Functions.Like(a.Audit.IpAddress ?? "", pattern) ||
                    EF.Functions.Like(a.UserEmail ?? "", pattern)
                );
            }

            auditQuery = auditQuery.ApplyAuditTrailSorting(pagedFilterRequest.SortBy, pagedFilterRequest.SortDirection);

            var totalCount = await auditQuery.CountAsync(ct);

            var auditTrails = await auditQuery
                .Skip((pagedFilterRequest.PageNumber - 1) * pagedFilterRequest.PageSize)
                .Take(pagedFilterRequest.PageSize)
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

            var pagedResult = PagedResult<AuditTrailResponse>.Create(
                auditTrails,
                totalCount,
                pagedFilterRequest.PageNumber,
                pagedFilterRequest.PageSize);

            return ResponseWrapper<PagedResult<AuditTrailResponse>>.Success(pagedResult);
        }
    }
}