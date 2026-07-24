namespace WFAI.Application.Features.Categories
{
    public static class CategoryCacheKeys
    {
        public static string GetAll(bool? isActive) => $"categories:all:{isActive?.ToString().ToLowerInvariant() ?? "null"}";
        public static string GetAllAdmin => "categories:allAdmin";
        public static string GetAllForList => "categories:allForList";

        public static IEnumerable<string> All =>
            new[]
            {
                GetAll(null),
                GetAll(true),
                GetAll(false),
                GetAllAdmin,
                GetAllForList,
            };
    }
}