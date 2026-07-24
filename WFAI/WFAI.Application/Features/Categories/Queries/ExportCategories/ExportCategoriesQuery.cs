using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mediator;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Categories.Queries.ExportCategories
{
    public sealed record ExportCategoriesQuery : IRequest<IResponseWrapper<byte[]>>
    {
        public string? SearchTerm { get; init; }
        public bool? IsActive { get; init; }
        public string? SortBy { get; init; }
        public string? SortDirection { get; init; }
        public string ExportFormat { get; init; } = "excel";
        public bool IncludeDeleted { get; init; } = false;
    }

    public sealed class ExportCategoriesQueryHandler(
        IApplicationDbContext applicationDbContext,
        ICurrentUserService currentUserService,
        ICategoryExportService categoryExportService)
        : IRequestHandler<ExportCategoriesQuery, IResponseWrapper<byte[]>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly ICategoryExportService _categoryExportService = categoryExportService;

        public async ValueTask<IResponseWrapper<byte[]>> Handle(ExportCategoriesQuery request, CancellationToken ct)
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

            var fileBytes = await _categoryExportService.ExportCategoriesAsync(categories, request.ExportFormat, ct);

            return ResponseWrapper<byte[]>.Success(fileBytes);
        }
    }
}