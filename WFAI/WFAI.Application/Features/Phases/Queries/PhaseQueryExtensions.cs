using Microsoft.EntityFrameworkCore;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Phases.Queries
{
    public static class PhaseQueryExtensions
    {
        public static IQueryable<Phase> ApplyPhaseFilters(
            this IQueryable<Phase> query,
            ICurrentUserService currentUserService,
            string? searchTerm,
            bool? isActive,
            bool includeDeleted = false)
        {
            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }
            else
            {
                if (!currentUserService.IsAuthenticated() || !currentUserService.HasClaim("permission", "Permission.Product.Phases.Read"))
                {
                    query = query.Where(c => c.IsActive);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                var pattern = $"%{term}%";
                query = query.Where(c =>
                    EF.Functions.Like(c.Title, pattern) ||
                    (c.Description != null && EF.Functions.Like(c.Description, pattern)));
            }

            return query;
        }

        public static IQueryable<Phase> ApplyPhaseSorting(
            this IQueryable<Phase> query,
            string? sortBy,
            string? sortDirection)
        {
            return sortBy?.ToLower() switch
            {
                "title" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.Title)
                    : query.OrderBy(c => c.Title),
                "sortorder" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.SortOrder)
                    : query.OrderBy(c => c.SortOrder),
                "id" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.Id)
                    : query.OrderBy(c => c.Id),
                _ => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.SortOrder).ThenBy(c => c.Title)
                    : query.OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            };
        }
    }
}
