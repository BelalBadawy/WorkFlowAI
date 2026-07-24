using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WFAI.API.Extensions;
using WFAI.Application.Authorization;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged;
using WFAI.Application.Features.AuditTrails.Queries.ExportAuditTrails;

namespace WFAI.API.Endpoints
{
    public static class AuditTrailEndpoints
    {
        public static IEndpointRouteBuilder MapAuditTrailEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/v{version:apiVersion}/audit-logs")
                .WithTags("AuditLogs");

            group.MapGet("paged", async (
                ISender sender, 
                [AsParameters] PagedFilterRequest filter, 
                string? tableName,
                string? entityId,
                string? actionTypes,
                string? fromDate,
                string? toDate,
                int? userId,
                CancellationToken ct) =>
            {
                var query = new GetAuditTrailsPagedQuery 
                {
                    PagedFilterRequest = filter,
                    TableName = tableName,
                    EntityId = entityId,
                    ActionTypes = actionTypes,
                    FromDate = fromDate,
                    ToDate = toDate,
                    UserId = userId
                };
                var response = await sender.Send(query, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<PagedResult<AuditTrailResponse>>>()
            .WithName("GetAuditTrailsPaged")
            .RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.AuditTrails, AppAction.Read));

            group.MapGet("/export", async (
                ISender sender,
                string? tableName,
                string? entityId,
                string? actionTypes,
                string? fromDate,
                string? toDate,
                int? userId,
                string? exportFormat,
                CancellationToken ct) =>
            {
                var query = new ExportAuditTrailsQuery
                {
                    TableName = tableName,
                    EntityId = entityId,
                    ActionTypes = actionTypes,
                    FromDate = fromDate,
                    ToDate = toDate,
                    UserId = userId,
                    ExportFormat = exportFormat ?? "excel"
                };
                var response = await sender.Send(query, ct);
                if (!response.IsSuccessful || response.Data == null)
                {
                    return response.ToApiResult();
                }

                var isPdf = (exportFormat ?? "").Equals("pdf", StringComparison.OrdinalIgnoreCase);
                var contentType = isPdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var extension = isPdf ? "pdf" : "xlsx";
                var fileName = $"AuditLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";

                return Results.File(response.Data, contentType, fileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest)
            .WithName("ExportAuditTrails")
            .RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.AuditTrails, AppAction.Read));

            return app;
        }
    }
}