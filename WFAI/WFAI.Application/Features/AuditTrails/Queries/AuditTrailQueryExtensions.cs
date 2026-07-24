using System;
using System.Globalization;
using System.Linq;
using WFAI.Domain.Entities;
using WFAI.Domain.Enums;

namespace WFAI.Application.Features.AuditTrails.Queries
{
    public class AuditTrailQueryModel
    {
        public AuditTrail Audit { get; set; } = null!;
        public string? UserEmail { get; set; }
    }

    public static class AuditTrailQueryExtensions
    {
        public static IQueryable<AuditTrailQueryModel> ApplyAuditTrailFilters(
            this IQueryable<AuditTrailQueryModel> query,
            int? userId,
            string? tableName,
            string? entityId,
            string? actionTypes,
            string? fromDate,
            string? toDate)
        {
            // Conditionally filter by UserId (exact match)
            if (userId.HasValue)
            {
                query = query.Where(a => a.Audit.UserId == userId.Value);
            }

            // Conditionally filter by TableName (exact match)
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                query = query.Where(a => a.Audit.TableName == tableName.Trim());
            }

            // Conditionally filter by EntityId (PrimaryKey substring search)
            if (!string.IsNullOrWhiteSpace(entityId))
            {
                var idTrimmed = entityId.Trim();
                query = query.Where(a => a.Audit.PrimaryKey != null && a.Audit.PrimaryKey.Contains(idTrimmed));
            }

            // Conditionally filter by ActionTypes list
            if (!string.IsNullOrWhiteSpace(actionTypes))
            {
                var typesList = actionTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => Enum.TryParse<AuditType>(t.Trim(), true, out var result) ? result : (AuditType?)null)
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value)
                    .ToList();

                if (typesList.Count > 0)
                {
                    query = query.Where(a => typesList.Contains(a.Audit.Type));
                }
            }

            // Conditionally filter by FromDate (inclusive)
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParseExact(fromDate.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFromDate))
            {
                query = query.Where(a => a.Audit.DateTime >= parsedFromDate);
            }

            // Conditionally filter by ToDate (inclusive, adjusted to end of day)
            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParseExact(toDate.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedToDate))
            {
                var adjustedToDate = parsedToDate.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Audit.DateTime <= adjustedToDate);
            }

            return query;
        }

        public static IQueryable<AuditTrailQueryModel> ApplyAuditTrailSorting(
            this IQueryable<AuditTrailQueryModel> query,
            string? sortBy,
            string? sortDirection)
        {
            return sortBy?.ToLower() switch
            {
                "tablename" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(a => a.Audit.TableName)
                    : query.OrderBy(a => a.Audit.TableName),
                "type" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(a => a.Audit.Type)
                    : query.OrderBy(a => a.Audit.Type),
                "datetime" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(a => a.Audit.DateTime)
                    : query.OrderBy(a => a.Audit.DateTime),
                "id" => (sortDirection ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(a => a.Audit.Id)
                    : query.OrderBy(a => a.Audit.Id),
                _ => (sortDirection ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderBy(a => a.Audit.DateTime).ThenBy(a => a.Audit.Id)
                    : query.OrderByDescending(a => a.Audit.DateTime).ThenByDescending(a => a.Audit.Id)
            };
        }
    }
}