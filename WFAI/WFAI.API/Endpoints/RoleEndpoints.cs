using Mediator;
using WFAI.API.Extensions;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Roles;
using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Features.Roles.Queries;
using WFAI.Application.Authorization;

namespace WebApi.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v{version:apiVersion}/roles")
                       .WithTags("Roles")
                       .RequireAuthorization();

        group.MapPost("/", async (CreateRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new CreateRoleCommand { CreateRole = request }, ct);
            return response.ToApiResult();
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Create))
          .Produces<IResponseWrapper>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest);

        group.MapGet("all", async (ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new GetRolesQuery(), ct);
            return response.IsSuccessful ? Results.Ok(response) : Results.NotFound(response);
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read))
          .Produces<IResponseWrapper<List<RoleResponse>>>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status404NotFound);

        group.MapPut("/", async (UpdateRoleRequest updateRole, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new UpdateRoleCommand { UpdateRole = updateRole }, ct);
            return response.ToApiResult();
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Update))
          .Produces<IResponseWrapper>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest);

        group.MapGet("{roleId:int}", async (int roleId, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new GetRoleByIdQuery { RoleId = roleId }, ct);
            return response.IsSuccessful ? Results.Ok(response) : Results.NotFound(response);
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read))
          .Produces<IResponseWrapper<RoleResponse>>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status404NotFound);

        group.MapDelete("{roleId:int}", async (int roleId, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new DeleteRoleCommand { RoleId = roleId }, ct);
            return response.ToApiResult();
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Delete))
          .Produces<IResponseWrapper>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest);

        group.MapGet("permissions/{roleId:int}", async (int roleId, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new GetPermissionsQuery { RoleId = roleId }, ct);
            return response.IsSuccessful ? Results.Ok(response) : Results.NotFound(response);
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Read))
          .Produces<IResponseWrapper<RoleClaimResponse>>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status404NotFound);

        group.MapPut("update-permissions", async (UpdateRoleClaimsRequest request, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new UpdateRolePermissionsCommand { UpdateRoleClaims = request }, ct);
            return response.ToApiResult();
        }).RequireAuthorization(AppPermission.NameFor(AppService.Identity, AppFeature.Roles, AppAction.Update))
          .Produces<IResponseWrapper>(StatusCodes.Status200OK)
          .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest);

        return app;
    }
}