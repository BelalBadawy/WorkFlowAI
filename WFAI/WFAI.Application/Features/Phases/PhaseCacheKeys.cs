namespace WFAI.Application.Features.Phases
{
    public static class PhaseCacheKeys
    {
        public static string GetAll(bool? isActive) => $"phases:all:{isActive?.ToString().ToLowerInvariant() ?? "null"}";
        public static string GetAllAdmin => "phases:allAdmin";

        public static IEnumerable<string> All =>
            new[]
            {
                GetAll(null),
                GetAll(true),
                GetAll(false),
                GetAllAdmin,
            };
    }
}
