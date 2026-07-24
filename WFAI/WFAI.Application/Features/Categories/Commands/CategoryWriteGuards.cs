namespace WFAI.Application.Features.Categories.Commands;

internal static class CategoryWriteGuards
{
    public static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    public static async Task<string?> ValidateParentAssignmentAsync(
        IApplicationDbContext dbContext,
        int? categoryId,
        int? parentId,
        CancellationToken ct)
    {
        if (!parentId.HasValue)
        {
            return null;
        }

        if (categoryId.HasValue && categoryId.Value == parentId.Value)
        {
            return "A category cannot be its own parent.";
        }

        var visited = new HashSet<int>();
        var currentParentId = parentId.Value;

        while (true)
        {
            if (!visited.Add(currentParentId))
            {
                return "Category hierarchy contains a cycle. Please select a valid parent category.";
            }

            if (categoryId.HasValue && currentParentId == categoryId.Value)
            {
                return "A category cannot be assigned to one of its descendants.";
            }

            var parentNode = await dbContext.Categories
                .Where(x => x.Id == currentParentId)
                .Select(x => new { x.Id, x.ParentId })
                .FirstOrDefaultAsync(ct);

            if (parentNode is null)
            {
                return "Selected parent category does not exist.";
            }

            if (!parentNode.ParentId.HasValue)
            {
                return null;
            }

            currentParentId = parentNode.ParentId.Value;
        }
    }

    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        var dbError = exception.InnerException?.Message ?? exception.Message;

        return dbError.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || dbError.Contains("UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase)
            || dbError.Contains("unique index", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetUniqueConstraintMessage(DbUpdateException exception)
    {
        var dbError = exception.InnerException?.Message ?? string.Empty;

        if (dbError.Contains("UX_Categories_NormalizedName", StringComparison.OrdinalIgnoreCase))
        {
            return "Category with this name already exists.";
        }

        if (dbError.Contains("UX_Categories_NormalizedSlug", StringComparison.OrdinalIgnoreCase))
        {
            return "Category with this slug already exists.";
        }

        return "Category with the same identity data already exists.";
    }
}