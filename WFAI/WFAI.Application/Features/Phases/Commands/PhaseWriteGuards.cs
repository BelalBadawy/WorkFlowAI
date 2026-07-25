using Microsoft.EntityFrameworkCore;

namespace WFAI.Application.Features.Phases.Commands
{
    internal static class PhaseWriteGuards
    {
        public static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

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

            if (dbError.Contains("UX_Phases_NormalizedTitle", StringComparison.OrdinalIgnoreCase))
            {
                return "Phase with this title already exists.";
            }

            return "Phase with the same identity data already exists.";
        }
    }
}
