using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Categories.Queries.GetCategoriesList
{
    public sealed record GetCategoriesListQuery(
        string? SearchTerm,
        bool? IsActive,
        string? SortBy,
        string? SortDirection,
        bool IncludeDeleted = false
    ) : IRequest<IResponseWrapper<List<CategoryResponse>>>;

    public sealed class GetCategoriesListQueryHandler(
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService)
        : IRequestHandler<GetCategoriesListQuery, IResponseWrapper<List<CategoryResponse>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async ValueTask<IResponseWrapper<List<CategoryResponse>>> Handle(GetCategoriesListQuery request, CancellationToken ct)
        {
            var categories = await _applicationDbContext.Categories
                .AsNoTracking()
                .ApplyCategoryFilters(_currentUserService, request.SearchTerm, request.IsActive, request.IncludeDeleted)
                .ApplyCategorySorting(request.SortBy, request.SortDirection)
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.ParentId,
                    c.SortOrder,
                    c.IsActive,
                    c.SoftDeleted,
                    c.RowVersion
                ))
                .ToListAsync(ct);

            return ResponseWrapper<List<CategoryResponse>>.Success(categories);
        }
    }
}