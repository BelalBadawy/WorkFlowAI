using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;

namespace WFAI.Application.Features.Categories.Queries
{
    public static class CategoryQueryExtensions
    {
        public static IQueryable<Category> ApplyCategoryFilters(
            this IQueryable<Category> query,
            ICurrentUserService currentUserService,
            string? searchTerm,
            bool? isActive,
            bool includeDeleted = false)
        {
            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            // Status Filtering
            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }
            else
            {
                // For anonymous or non-privileged requests, show only active categories.
                if (!currentUserService.IsAuthenticated() || !currentUserService.HasClaim("permission", "Permission.Product.Categories.Read"))
                {
                    query = query.Where(c => c.IsActive);
                }
            }

            // Search Term Filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                var pattern = $"%{term}%";
                query = query.Where(c =>
                    EF.Functions.Like(c.Name, pattern) ||
                    EF.Functions.Like(c.Slug, pattern));
            }

            return query;
        }

        public static IQueryable<Category> ApplyCategorySorting(
            this IQueryable<Category> query,
            string? sortBy,
            string? sortDirection)
        {
            return sortBy?.ToLower() switch
            {
                "name" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),
                "slug" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.Slug)
                    : query.OrderBy(c => c.Slug),
                "sortorder" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.SortOrder)
                    : query.OrderBy(c => c.SortOrder),
                "id" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.Id)
                    : query.OrderBy(c => c.Id),
                _ => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(c => c.SortOrder).ThenBy(c => c.Name)
                    : query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            };
        }
    }
}