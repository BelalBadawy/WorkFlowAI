using Mediator;
using WFAI.API.Extensions;
using WFAI.Application.Authorization;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Phases;
using WFAI.Application.Features.Phases.Commands.ChangePhaseStatus;
using WFAI.Application.Features.Phases.Commands.Create;
using WFAI.Application.Features.Phases.Commands.Delete;
using WFAI.Application.Features.Phases.Commands.RestorePhase;
using WFAI.Application.Features.Phases.Commands.Update;
using WFAI.Application.Features.Phases.Queries.ExportPhases;
using WFAI.Application.Features.Phases.Queries.GetPhaseById;
using WFAI.Application.Features.Phases.Queries.GetPhasesPaged;

namespace WFAI.API.Endpoints
{
    public static class PhaseEndpoints
    {
        public static IEndpointRouteBuilder MapPhaseEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/v{version:apiVersion}/phases")
                .WithTags("Phases");

            group.MapGet("/paged", async (ISender sender, [AsParameters] PagedFilterRequest filter, bool includeDeleted = false, CancellationToken ct = default) =>
            {
                var query = new GetPhasesPagedQuery { PagedFilterRequest = filter, IncludeDeleted = includeDeleted };
                var response = await sender.Send(query, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<PagedResult<PhaseDto>>>()
            .WithName("GetPhasesPaged")
            .AllowAnonymous();

            group.MapGet("/export", async (
                ISender sender,
                string? searchTerm,
                bool? isActive,
                string? sortBy,
                string? sortDirection,
                string? exportFormat,
                CancellationToken ct) =>
            {
                var query = new ExportPhasesQuery
                {
                    SearchTerm = searchTerm,
                    IsActive = isActive,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
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
                var fileName = $"Phases_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";

                return Results.File(response.Data, contentType, fileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest)
            .WithName("ExportPhases")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Read));

            group.MapGet("/{phaseId:int}", async (ISender sender, int phaseId, CancellationToken ct) =>
            {
                var query = new GetPhaseByIdQuery(phaseId);
                var response = await sender.Send(query, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<PhaseDto>>()
            .WithName("GetPhaseById")
            .AllowAnonymous();

            group.MapPost("/", async (ISender sender, CreatePhaseRequest request, CancellationToken ct) =>
            {
                var command = new CreatePhaseCommand(
                    request.Title,
                    request.Description,
                    request.IsActive,
                    request.SortOrder);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<int>>()
            .WithName("CreatePhase")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Create));

            group.MapPut("/", async (ISender sender, UpdatePhaseCommand request, CancellationToken ct) =>
            {
                var response = await sender.Send(request, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper>()
            .WithName("UpdatePhase")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Update));

            group.MapPut("/{id:int}/status", async (int id, bool isActive, ISender sender, CancellationToken ct) =>
            {
                var command = new ChangePhaseStatusCommand(id, isActive);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<int>>()
            .WithName("ChangePhaseStatus")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Update));

            group.MapDelete("/{phaseId:int}", async (ISender sender, int phaseId, CancellationToken ct) =>
            {
                var command = new DeletePhaseCommand(phaseId);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper>()
            .WithName("DeletePhase")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Delete));

            group.MapPost("/{id:int}/restore", async (int id, ISender sender, CancellationToken ct) =>
            {
                var command = new RestorePhaseCommand(id);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<int>>()
            .WithName("RestorePhase")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Update));

            return app;
        }
    }
}
