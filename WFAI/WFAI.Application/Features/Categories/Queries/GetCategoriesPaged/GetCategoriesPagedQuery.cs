using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Categories.Queries.GetCategoriesPaged
{
    public record CategoryResponse(
        int Id,
        string Name,
        string Slug,
        int? ParentId,
        int SortOrder,
        bool IsActive,
        bool SoftDeleted,
        byte[] RowVersion
    );

    public sealed record GetCategoriesPagedQuery : IRequest<IResponseWrapper<PagedResult<CategoryResponse>>>, IValidateMe
    {
        public PagedFilterRequest PagedFilterRequest { get; init; } = new();
        public bool IncludeDeleted { get; init; } = false;
    }

    public sealed class GetCategoriesPagedQueryHandler(
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService)
        : IRequestHandler<GetCategoriesPagedQuery, IResponseWrapper<PagedResult<CategoryResponse>>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async ValueTask<IResponseWrapper<PagedResult<CategoryResponse>>> Handle(GetCategoriesPagedQuery request, CancellationToken ct)
        {
            var pagedFilterRequest = request.PagedFilterRequest;
            var query = _applicationDbContext.Categories
                .AsNoTracking()
                .ApplyCategoryFilters(_currentUserService, pagedFilterRequest.SearchTerm, pagedFilterRequest.IsActive, request.IncludeDeleted)
                .ApplyCategorySorting(pagedFilterRequest.SortBy, pagedFilterRequest.SortDirection);

            var totalCount = await query.CountAsync(ct);

            var categories = await query
                .Skip((pagedFilterRequest.PageNumber - 1) * pagedFilterRequest.PageSize)
                .Take(pagedFilterRequest.PageSize)
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

            var pagedResult = PagedResult<CategoryResponse>.Create(
                categories,
                totalCount,
                pagedFilterRequest.PageNumber,
                pagedFilterRequest.PageSize);

            return ResponseWrapper<PagedResult<CategoryResponse>>.Success(pagedResult);
        }
    }
}