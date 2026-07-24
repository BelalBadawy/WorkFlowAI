using Mediator;
using Microsoft.AspNetCore.Mvc;
using WFAI.API.Extensions;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Categories.Commands.Create;
using WFAI.Application.Features.Categories.Commands.Delete;
using WFAI.Application.Features.Categories.Commands.Update;
using WFAI.Application.Features.Categories.Commands.ChangeCategoryStatus;
using WFAI.Application.Features.Categories.Queries.GetAllCategories;
using WFAI.Application.Features.Categories.Queries.GetAllCategoriesForList;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;
using WFAI.Application.Features.Categories.Queries.GetCategoryById;
using WFAI.Application.Features.Categories.Queries.ExportCategories;
using WFAI.Application.Features.Categories.Commands.RestoreCategory;
using WFAI.Application.Authorization;

namespace WFAI.API.Endpoints
{
    public static class CategoryEndpoints
    {
        public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/v{version:apiVersion}/categories")
                .WithTags("Categories");

            group.MapGet("/", async (ISender sender, bool? isActive, CancellationToken ct) =>
            {
                var query = new GetAllCategoriesQuery(isActive);
                var response = await sender.Send(query, ct);
                  return response.ToApiResult(); 
            })
            .Produces<IResponseWrapper<List<CategoryResponse>>>()
            .WithName("GetAllCategories")
            .AllowAnonymous();

            group.MapGet("/paged", async (ISender sender, [AsParameters] PagedFilterRequest filter, bool includeDeleted = false, CancellationToken ct = default) =>
            {
                // Use object initializer syntax instead of a constructor
                var query = new GetCategoriesPagedQuery { PagedFilterRequest = filter, IncludeDeleted = includeDeleted };
                var response = await sender.Send(query, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<PagedResult<CategoryResponse>>>()
            .WithName("GetCategoriesPaged")
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
                var query = new ExportCategoriesQuery
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
                var fileName = $"Categories_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";

                return Results.File(response.Data, contentType, fileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .Produces<IResponseWrapper>(StatusCodes.Status400BadRequest)
            .WithName("ExportCategories")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Read));

            group.MapGet("/for-list", async (ISender sender, CancellationToken ct) =>
            {
                var query = new GetAllCategoriesForListQuery();
                var response = await sender.Send(query, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<List<CategoryLookupDto>>>()
            .WithName("GetCategoriesForList")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Read));

            group.MapGet("/{categoryId:int}", async (ISender sender, int categoryId, CancellationToken ct) =>
            {
                var query = new GetCategoryByIdQuery(categoryId);
                var response = await sender.Send(query, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<CategoryResponse>>()
            .WithName("GetCategoryById")
            .AllowAnonymous();

            group.MapPost("/", async (ISender sender, CreateCategoryRequest request, CancellationToken ct) =>
            {
                var command = new CreateCategoryCommand(
                    request.Name,
                    request.Slug,
                    request.ParentId,
                    request.IsActive,
                    request.SortOrder);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper>()
            .WithName("CreateCategory")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Create));

            group.MapPut("/", async (ISender sender, UpdateCategoryCommand request, CancellationToken ct) =>
            {
                var response = await sender.Send(request, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper>()
            .WithName("UpdateCategory")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Update));

            group.MapPut("/{id:int}/status", async (int id, bool isActive, ISender sender, CancellationToken ct) =>
            {
                var command = new ChangeCategoryStatusCommand(id, isActive);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<int>>()
            .WithName("ChangeCategoryStatus")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Update));

            group.MapDelete("/{categoryId:int}", async (ISender sender, int categoryId, CancellationToken ct) =>
            {
                var command = new DeleteCategoryCommand(categoryId);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper>()
            .WithName("DeleteCategory")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Delete));

            group.MapPost("/{id:int}/restore", async (int id, ISender sender, CancellationToken ct) =>
            {
                var command = new RestoreCategoryCommand(id);
                var response = await sender.Send(command, ct);
                return response.ToApiResult();
            })
            .Produces<IResponseWrapper<int>>()
            .WithName("RestoreCategory")
            .RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Update));

            return app;
        }
    }
}